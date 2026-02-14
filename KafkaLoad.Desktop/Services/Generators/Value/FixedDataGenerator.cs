using System;
using System.Text;

namespace KafkaLoad.Desktop.Services.Generators.Value
{
    public class FixedDataGenerator : IDataGenerator
    {
        private readonly byte[] _data;

        public FixedDataGenerator(string? data)
        {
            _data = data != null ? Encoding.UTF8.GetBytes(data) : Array.Empty<byte>();
        }

        public byte[] Next() => _data;
    }
}
