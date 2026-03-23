using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Interfaces;

public interface IFileManager
{
    Task SaveAsync<T>(T config, string filePath);
    Task<T?> LoadAsync<T>(string filePath);
}
