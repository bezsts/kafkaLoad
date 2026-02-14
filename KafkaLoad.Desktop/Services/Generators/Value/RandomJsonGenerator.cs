using System;
using System.Text.Json;

namespace KafkaLoad.Desktop.Services.Generators.Value
{
    public class RandomJsonGenerator : IDataGenerator
    {
        private readonly int _approxSize;

        public RandomJsonGenerator(int size)
        {
            _approxSize = size;
        }

        public byte[] Next()
        {
            // Simple dynamic JSON generation
            // For exact size match, logic would be more complex (padding).
            // TODO: check for more advanced implementation

            var data = new
            {
                Timestamp = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                Payload = new string('x', Math.Max(0, _approxSize - 50))
            };

            return JsonSerializer.SerializeToUtf8Bytes(data);
        }
    }
}
