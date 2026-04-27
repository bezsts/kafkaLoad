using Google.Protobuf;
using Google.Protobuf.Reflection;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Generators.Value;

public class ProtobufSchemaGenerator : IDataGenerator
{
    private readonly DescriptorProto _messageDescriptor;

    public ProtobufSchemaGenerator(string protoText)
    {
        if (string.IsNullOrEmpty(protoText))
            throw new ArgumentException("Proto schema must not be null or empty.", nameof(protoText));

        var descriptorSet = CompileProto(protoText);
        if (descriptorSet.File.Count == 0 || descriptorSet.File[0].MessageType.Count == 0)
            throw new InvalidOperationException("No message types found in the proto schema.");

        _messageDescriptor = descriptorSet.File[0].MessageType[0];
        Log.Information("Protobuf generator ready — message: {MessageName}", _messageDescriptor.Name);
    }

    public byte[]? Next()
    {
        using var ms = new MemoryStream();
        var output = new CodedOutputStream(ms);

        foreach (var field in _messageDescriptor.Field)
        {
            // Skip repeated fields (write as empty for now)
            if (field.Label == FieldDescriptorProto.Types.Label.Repeated)
                continue;

            WriteField(output, field);
        }

        output.Flush();
        return ms.ToArray();
    }

    private static void WriteField(CodedOutputStream output, FieldDescriptorProto field)
    {
        switch (field.Type)
        {
            case FieldDescriptorProto.Types.Type.String:
                output.WriteTag(field.Number, WireFormat.WireType.LengthDelimited);
                output.WriteString(Guid.NewGuid().ToString());
                break;
            case FieldDescriptorProto.Types.Type.Bytes:
                output.WriteTag(field.Number, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.Empty);
                break;
            case FieldDescriptorProto.Types.Type.Int32:
            case FieldDescriptorProto.Types.Type.Sint32:
            case FieldDescriptorProto.Types.Type.Enum:
                output.WriteTag(field.Number, WireFormat.WireType.Varint);
                output.WriteInt32(Random.Shared.Next(1, 100_000));
                break;
            case FieldDescriptorProto.Types.Type.Uint32:
                output.WriteTag(field.Number, WireFormat.WireType.Varint);
                output.WriteUInt32((uint)Random.Shared.Next(1, 100_000));
                break;
            case FieldDescriptorProto.Types.Type.Int64:
            case FieldDescriptorProto.Types.Type.Sint64:
                output.WriteTag(field.Number, WireFormat.WireType.Varint);
                output.WriteInt64(Random.Shared.NextInt64(1, 1_000_000_000L));
                break;
            case FieldDescriptorProto.Types.Type.Uint64:
                output.WriteTag(field.Number, WireFormat.WireType.Varint);
                output.WriteUInt64((ulong)Random.Shared.NextInt64(1, 1_000_000_000L));
                break;
            case FieldDescriptorProto.Types.Type.Bool:
                output.WriteTag(field.Number, WireFormat.WireType.Varint);
                output.WriteBool(Random.Shared.Next(2) == 0);
                break;
            case FieldDescriptorProto.Types.Type.Float:
                output.WriteTag(field.Number, WireFormat.WireType.Fixed32);
                output.WriteFloat((float)Random.Shared.NextDouble() * 1000f);
                break;
            case FieldDescriptorProto.Types.Type.Fixed32:
            case FieldDescriptorProto.Types.Type.Sfixed32:
                output.WriteTag(field.Number, WireFormat.WireType.Fixed32);
                output.WriteFixed32((uint)Random.Shared.Next());
                break;
            case FieldDescriptorProto.Types.Type.Double:
                output.WriteTag(field.Number, WireFormat.WireType.Fixed64);
                output.WriteDouble(Random.Shared.NextDouble() * 1000.0);
                break;
            case FieldDescriptorProto.Types.Type.Fixed64:
            case FieldDescriptorProto.Types.Type.Sfixed64:
                output.WriteTag(field.Number, WireFormat.WireType.Fixed64);
                output.WriteFixed64((ulong)Random.Shared.NextInt64());
                break;
            // Nested messages: write empty length-delimited bytes
            case FieldDescriptorProto.Types.Type.Message:
                output.WriteTag(field.Number, WireFormat.WireType.LengthDelimited);
                output.WriteBytes(ByteString.Empty);
                break;
        }
    }

    public static async Task<(bool ok, string? error)> ValidateAsync(string protoText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(protoText))
            return (true, null);

        try
        {
            await Task.Run(() => CompileProto(protoText), ct);
            return (true, null);
        }
        catch (OperationCanceledException)
        {
            return (true, null);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            const string prefix = "protoc compilation failed:\n";
            if (msg.StartsWith(prefix, StringComparison.Ordinal))
                msg = msg[prefix.Length..].Trim();
            return (false, msg);
        }
    }

    private static FileDescriptorSet CompileProto(string protoText)
    {
        var protocPath = FindProtoc();
        var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var baseName = $"kafkaload_{Guid.NewGuid():N}";
        var protoFile = Path.Combine(tempDir, baseName + ".proto");
        var descFile  = Path.Combine(tempDir, baseName + ".desc");

        try
        {
            File.WriteAllText(protoFile, protoText);

            var psi = new ProcessStartInfo
            {
                FileName = protocPath,
                Arguments = $"--descriptor_set_out=\"{descFile}\" --proto_path=\"{tempDir}\" \"{Path.GetFileName(protoFile)}\"",
                RedirectStandardError  = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow  = true
            };

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start protoc process.");

            proc.WaitForExit(15_000);

            if (proc.ExitCode != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                throw new InvalidOperationException($"protoc compilation failed:\n{err}");
            }

            var bytes = File.ReadAllBytes(descFile);
            return FileDescriptorSet.Parser.ParseFrom(bytes);
        }
        finally
        {
            TryDelete(protoFile);
            TryDelete(descFile);
        }
    }

    private static string FindProtoc()
    {
        var exeDir = AppContext.BaseDirectory;
        var name   = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "protoc.exe" : "protoc";
        var path   = Path.Combine(exeDir, "tools", name);

        if (File.Exists(path))
            return path;

        throw new FileNotFoundException(
            $"Embedded protoc not found at '{path}'. " +
            $"Ensure the project was built correctly so protoc is copied to the output directory.");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort */ }
    }
}
