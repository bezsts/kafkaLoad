namespace KafkaLoad.Desktop.Services.Generators
{
    public interface IDataGenerator
    {
        byte[]? Next();
    }
}