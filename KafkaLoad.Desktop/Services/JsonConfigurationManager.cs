using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using KafkaLoad.Desktop.Services.Interfaces;

namespace KafkaLoad.Desktop.Services;

public class JsonConfigurationManager : IConfigurationManager
{
    private readonly JsonSerializerOptions _options = new() 
    { 
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    public async Task<T?> LoadAsync<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;

        using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream, _options);
    }

    public async Task SaveAsync<T>(T config, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, config, _options);
    }
}
