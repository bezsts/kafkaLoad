using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System.Buffers;
using System.Text.Json;
namespace KafkaLoad.Core.Services.Generators.Value
{
    public class RandomJsonGenerator : IDataGenerator
    {
        private readonly byte[][] _pool;
        private readonly int _poolSize;
        private int _index = -1;

        public RandomJsonGenerator(int size)
        {
            int targetSize = Math.Max(0, size);

            int baseSize = MeasureBaseSize();

            int paddingSize = Math.Max(0, targetSize - baseSize);
            if (targetSize > 0 && paddingSize == 0)
            {
                Log.Warning(
                    "RandomJsonGenerator: targetSize {TargetSize} is smaller than minimum JSON overhead ({BaseSize} bytes). " +
                    "Messages will be approximately {BaseSize} bytes.",
                    targetSize, baseSize);
            }

            int effectiveSize = baseSize + paddingSize;
            _poolSize = effectiveSize > 0
                ? Math.Max(8, Math.Min(512, 4 * 1024 * 1024 / effectiveSize))
                : 8;

            _pool = new byte[_poolSize][];
            for (int i = 0; i < _poolSize; i++)
                _pool[i] = BuildMessage(paddingSize);
        }

        public byte[] Next()
        {
            int idx = (int)((uint)Interlocked.Increment(ref _index) % (uint)_poolSize);
            return _pool[idx];
        }

        private static int MeasureBaseSize()
        {
            var buf = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buf);
            WriteMessage(writer, string.Empty);
            writer.Flush();
            return buf.WrittenCount;
        }

        private static byte[] BuildMessage(int paddingSize)
        {
            var buf = new ArrayBufferWriter<byte>(initialCapacity: 256 + paddingSize);
            using var writer = new Utf8JsonWriter(buf);
            WriteMessage(writer, paddingSize > 0 ? new string('x', paddingSize) : string.Empty);
            writer.Flush();
            return buf.WrittenSpan.ToArray();
        }

        private static void WriteMessage(Utf8JsonWriter writer, string payload)
        {
            writer.WriteStartObject();
            writer.WriteString("Timestamp"u8, DateTime.UtcNow);
            writer.WriteString("Id"u8, Guid.NewGuid());
            writer.WriteString("Payload"u8, payload);
            writer.WriteEndObject();
        }
    }
}
