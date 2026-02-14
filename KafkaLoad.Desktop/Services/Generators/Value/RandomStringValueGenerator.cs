using System;
using System.Text;

namespace KafkaLoad.Desktop.Services.Generators.Value
{
    public class RandomStringValueGenerator : IDataGenerator
    {
        private readonly int _size;
        private readonly Random _random = new();
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public RandomStringValueGenerator(int size)
        {
            _size = size;
        }

        public byte[] Next()
        {
            // Note: Generating random strings char-by-char is CPU intensive.
            // Optimally, for load testing, we pre-generate a large buffer and take slices,
            // but for simplicity, here is the direct implementation.
            // TODO: check another algorithm

            var stringChars = new char[_size];
            for (int i = 0; i < _size; i++)
            {
                stringChars[i] = Chars[_random.Next(Chars.Length)];
            }
            return Encoding.UTF8.GetBytes(new string(stringChars));
        }
    }
}
