using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models.Configs;
using KafkaLoad.Core.Models.Interfaces;
using System;

namespace KafkaLoad.Core.Models;

public class TestScenario : IConfigModel
{
    public string Name { get; set;} = string.Empty;
    public DateTime CreatedAt { get; set; }
    public CustomProducerConfig? ProducerConfig { get; set; }
    public CustomConsumerConfig? ConsumerConfig { get; set; }
    public KeyGenerationStrategy? KeyStrategy { get; set; }
    public ValueGenerationStrategy? ValueStrategy { get; set; }
    public TestType? TestType { get; set; }
    public string? FixedKey { get; set; }
    /// <summary>JSON template string used when <see cref="ValueStrategy"/> is <c>Json</c>.
    /// Supports placeholders: ${uuid}, ${randomInt}, ${randomInt:min:max}, ${randomString:N}, ${timestamp}, ${randomBool}</summary>
    public string? FixedTemplate { get; set; }
    public int? ProducerCount { get; set; } = 1;
    public int? ConsumerCount { get; set; } = 1;
    public int? MessageSize { get; set; } = 1024; 
    public int? Duration { get; set; } = 60;
    public int? TargetThroughput { get; set; }
    public int? BaseThroughput { get; set; }
    public int? SpikeThroughput { get; set; }

    //public CustomSecurityConfig Security { get; set; } = null!;
}
