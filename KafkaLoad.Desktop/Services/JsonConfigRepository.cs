using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class JsonConfigRepository<T> : IConfigRepository<T> where T : class
{
    private readonly IConfigRepository _fileManager;
    private readonly string _folderPath;

    public JsonConfigRepository(IConfigRepository fileManager, string folderName)
    {
        _fileManager = fileManager;
        _folderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Configs", folderName);
        
        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var files = Directory.GetFiles(_folderPath, "*.json");
        var configs = new List<T>();

        foreach (var file in files)
        {
            var config = await _fileManager.LoadAsync<T>(file);
            if (config != null)
            {
                configs.Add(config);
            }
        }
        return configs;
    }

    public async Task SaveAsync(T config)
    {
        dynamic dynamicConfig = config; 
        string fileName = $"{dynamicConfig.Name}.json";
        string fullPath = Path.Combine(_folderPath, fileName);

        await _fileManager.SaveAsync(config, fullPath);
    }

    public async Task DeleteAsync(string name)
    {
        string fullPath = Path.Combine(_folderPath, $"{name}.json");
        if (File.Exists(fullPath))
        {
            await Task.Run(() => File.Delete(fullPath));
        }
    }

    public Task<T?> GetByNameAsync(string name)
    {
        string fullPath = Path.Combine(_folderPath, $"{name}.json");
        return _fileManager.LoadAsync<T>(fullPath);
    }
}
