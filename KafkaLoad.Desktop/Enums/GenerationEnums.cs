namespace KafkaLoad.Desktop.Enums
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
        Fixed,          // User provided string/json repeated
        RandomString,   // Random alphanumeric text of specific size
        RandomJson      // JSON with random data inside
    }
}
