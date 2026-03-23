namespace KafkaLoad.Core.Services.Interfaces
{
    public interface IDataGenerator
    {
        byte[]? Next();
    }
}