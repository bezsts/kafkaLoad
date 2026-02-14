using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Generators.Keys;
using KafkaLoad.Desktop.Services.Generators.Value;

namespace KafkaLoad.Desktop.Services.Generators
{
    public class DataGeneratorFactory
    {
        public static IDataGenerator CreateKeyGenerator(TestScenario scenario)
        {
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
