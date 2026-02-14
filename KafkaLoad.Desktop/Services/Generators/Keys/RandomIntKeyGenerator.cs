using System;

namespace KafkaLoad.Desktop.Services.Generators.Keys
{
    public class RandomIntKeyGenerator : IDataGenerator
    {
        private readonly Random _random = new();

        public byte[] Next()
        {
            return BitConverter.GetBytes(_random.Next());
        }
    }
}
