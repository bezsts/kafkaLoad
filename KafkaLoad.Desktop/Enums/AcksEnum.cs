namespace KafkaLoad.Desktop.Enums;

public enum AcksEnum
{
    // Producer will not wait for any acknowledgment from the server.
    None = 0,

    // The leader will write the record to its local log but will not wait for full acknowledgement from all followers.
    Leader = 1,

    // The leader will wait for the full set of in-sync replicas to acknowledge the record.
    All = -1
}
