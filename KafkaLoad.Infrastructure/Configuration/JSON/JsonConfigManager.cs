using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;

namespace KafkaLoad.Infrastructure.Configuration.Json;

public class JsonConfigManager : IFileManager
{
    private readonly JsonSerializerOptions _options = new() 
    { 
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    public async Task<T?> LoadAsync<T>(string filePath)
    {
        if (!File.Exists(filePath)) 
        { 
            Log.Debug("File does not exist: {FilePath}", filePath);
            return default;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream, _options);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Failed to parse JSON file: {FilePath}", filePath);
            return default;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while reading file: {FilePath}", filePath);
            return default;
        }
    }

    public async Task SaveAsync<T>(T config, string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Log.Debug("Creating directory for configs: {Directory}", directory);
                Directory.CreateDirectory(directory);
            }

            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, config, _options);

            Log.Debug("Successfully saved config to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save configuration file to: {FilePath}", filePath);
            throw;
        }
    }
}
