using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models;
using KafkaLoad.Core.Services.Generators.Keys;
using KafkaLoad.Core.Services.Generators.Value;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;

namespace KafkaLoad.Core.Services.Generators
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
