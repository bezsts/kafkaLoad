namespace KafkaLoad.Desktop.Enums;

public enum AutoOffsetResetEnum
{
    // Automatically reset the offset to the latest offset.
    // You will only see NEW messages arriving after you start.
    Latest = 0,

    // Automatically reset the offset to the earliest offset.
    // You will read ALL history available in the topic.
    Earliest = 1,
}
