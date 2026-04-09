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
                KeyGenerationStrategy.Fixed => new FixedDataGenerator(scenario.FixedKey ?? "fixed-key"),
                _ => new FixedDataGenerator(null)
            };
        }

        public static IDataGenerator CreateValueGenerator(TestScenario scenario)
        {
            Log.Information("Initializing Value Generator with strategy: {Strategy}", scenario.ValueStrategy);

            return scenario.ValueStrategy switch
            {
                ValueGenerationStrategy.RandomString => new RandomStringValueGenerator(scenario.MessageSize ?? 1024),
                ValueGenerationStrategy.Json => new JsonTemplateGenerator(
                    scenario.FixedTemplate ?? throw new InvalidOperationException("Json strategy requires a non-null FixedTemplate.")),
                _ => new RandomStringValueGenerator(scenario.MessageSize ?? 1024)
            };
        }
    }
}
