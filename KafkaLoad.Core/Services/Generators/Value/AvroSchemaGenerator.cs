using Avro;
using Avro.Generic;
using Avro.IO;
using KafkaLoad.Core.Services.Interfaces;
using System;
using System.IO;

namespace KafkaLoad.Core.Services.Generators.Value;

public class AvroSchemaGenerator : IDataGenerator
{
    private readonly RecordSchema _schema;
    private readonly DatumWriter<GenericRecord> _writer;

    public AvroSchemaGenerator(string schemaJson)
    {
        if (string.IsNullOrEmpty(schemaJson))
            throw new ArgumentException("Avro schema must not be null or empty.", nameof(schemaJson));

        var parsed = Schema.Parse(schemaJson);
        if (parsed is not RecordSchema recordSchema)
            throw new ArgumentException("Avro schema must be a record type.", nameof(schemaJson));

        _schema = recordSchema;
        _writer = new GenericDatumWriter<GenericRecord>(_schema);
    }

    public byte[]? Next()
    {
        var record = new GenericRecord(_schema);
        foreach (var field in _schema.Fields)
            record.Add(field.Name, GenerateValue(field.Schema));

        using var ms = new MemoryStream();
        var encoder = new BinaryEncoder(ms);
        _writer.Write(record, encoder);
        encoder.Flush();
        return ms.ToArray();
    }

    private static object? GenerateValue(Schema schema) => schema.Tag switch
    {
        Schema.Type.String  => Guid.NewGuid().ToString(),
        Schema.Type.Int     => Random.Shared.Next(),
        Schema.Type.Long    => Random.Shared.NextInt64(),
        Schema.Type.Float   => (float)Random.Shared.NextDouble(),
        Schema.Type.Double  => Random.Shared.NextDouble(),
        Schema.Type.Boolean => Random.Shared.Next(2) == 0,
        Schema.Type.Null    => null,
        Schema.Type.Bytes   => Array.Empty<byte>(),
        Schema.Type.Union   => GenerateUnionValue((UnionSchema)schema),
        Schema.Type.Record  => GenerateRecord((RecordSchema)schema),
        _                   => null
    };

    private static object? GenerateUnionValue(UnionSchema union)
    {
        // Pick first non-null type, falling back to null
        foreach (var branch in union.Schemas)
        {
            if (branch.Tag != Schema.Type.Null)
                return GenerateValue(branch);
        }
        return null;
    }

    private static GenericRecord GenerateRecord(RecordSchema schema)
    {
        var record = new GenericRecord(schema);
        foreach (var field in schema.Fields)
            record.Add(field.Name, GenerateValue(field.Schema));
        return record;
    }
}
