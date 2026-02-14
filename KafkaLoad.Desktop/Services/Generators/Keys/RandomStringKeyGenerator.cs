using System;
using System.Text;

namespace KafkaLoad.Desktop.Services.Generators.Keys
{
    public class RandomStringKeyGenerator : IDataGenerator
    {
        public byte[] Next()
        {
            return Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
        }
    }
}
