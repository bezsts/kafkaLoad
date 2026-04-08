using KafkaLoad.Core.Services.Interfaces;
using System;
using System.Text;

namespace KafkaLoad.Core.Services.Generators.Value
{
    public class RandomStringValueGenerator : IDataGenerator
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private readonly int _size;
        private readonly char[] _pool;
        private readonly int _maxOffset;

        public RandomStringValueGenerator(int size)
        {
            _size = size;
            int poolSize = Math.Max(size * 16, 65536);
            _pool = new char[poolSize];
            for (int i = 0; i < poolSize; i++)
                _pool[i] = Chars[Random.Shared.Next(Chars.Length)];
            _maxOffset = poolSize - size;
        }

        public byte[] Next()
        {
            int offset = Random.Shared.Next(_maxOffset);
            return Encoding.UTF8.GetBytes(_pool, offset, _size);
        }
    }
}
