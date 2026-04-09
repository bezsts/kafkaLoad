namespace KafkaLoad.Core.Enums
{
    public enum KeyGenerationStrategy
    {
        Null,           // No key
        RandomInt,      // Random integer
        SequentialInt,  // 1, 2, 3...
        RandomString,   // Random UUID or alphanumeric string
        Fixed           // Always the same key
    }

    public enum ValueGenerationStrategy
    {
        RandomString,   // Random alphanumeric text of specific byte length
        Json            // JSON generated from user-defined template with placeholders
    }
}
