using System;

namespace KafkaLoad.Desktop.Services.Generators.Keys
{
    public class RandomIntKeyGenerator : IDataGenerator
    {
        private readonly Random _random = new();

        public byte[] Next()
        {
            byte[] bytes = BitConverter.GetBytes(_random.Next());

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }
}
