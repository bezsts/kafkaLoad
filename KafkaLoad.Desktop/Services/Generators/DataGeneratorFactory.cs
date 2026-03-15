using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Generators.Keys;
using KafkaLoad.Desktop.Services.Generators.Value;
using Serilog;

namespace KafkaLoad.Desktop.Services.Generators
{
    public class DataGeneratorFactory
    {
        public static IDataGenerator CreateKeyGenerator(TestScenario scenario)
        {
            Log.Information("Initializing Key Generator with strategy: {Strategy}", scenario.KeyStrategy);

            return scenario.KeyStrategy switch
            {
                KeyGenerationStrategy.SequentialInt => new SequentialIntKeyGenerator(),
                KeyGenerationStrategy.RandomInt => new RandomIntKeyGenerator(),
                KeyGenerationStrategy.RandomString => new RandomStringKeyGenerator(),
                KeyGenerationStrategy.Fixed => new FixedDataGenerator("fixed-key"),
                _ => new FixedDataGenerator(null)
            };
        }

        public static IDataGenerator CreateValueGenerator(TestScenario scenario)
        {
            int size = scenario.MessageSize ?? 1024;

            Log.Information("Initializing Value Generator with strategy: {Strategy}, Payload Size: {Size} bytes", scenario.ValueStrategy, size);

            return scenario.ValueStrategy switch
            {
                ValueGenerationStrategy.Fixed => new FixedDataGenerator(scenario.FixedTemplate ?? "default-payload"),
                ValueGenerationStrategy.RandomJson => new RandomJsonGenerator(size),
                ValueGenerationStrategy.RandomString => new RandomStringValueGenerator(size),
                _ => new RandomStringValueGenerator(size)
            };
        }
    }
}
