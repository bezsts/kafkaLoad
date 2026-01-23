using System;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface IConfigRepository
{
    Task SaveAsync<T>(T config, string filePath);
    Task<T?> LoadAsync<T>(string filePath);
}
