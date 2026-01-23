using System;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface IConfigManager
{
    Task SaveAsync<T>(T config, string filePath);
    Task<T?> LoadAsync<T>(string filePath);
}
