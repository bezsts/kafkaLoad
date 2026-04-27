using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KafkaLoad.Infrastructure.Configuration.Json;

public class JsonConfigRepository<T> : IConfigRepository<T> where T : class
{
    private readonly IFileManager _fileManager;
    private readonly string _folderPath;

    public JsonConfigRepository(IFileManager fileManager, string folderName)
    {
        _fileManager = fileManager;
        _folderPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Configs", folderName);
        
        if (!Directory.Exists(_folderPath))
        {
            Log.Information("Creating base configuration directory: {FolderPath}", _folderPath);
            Directory.CreateDirectory(_folderPath);
        }
    }

    public async Task<bool> ExistsAsync(string name)
    {
        string fullPath = Path.Combine(_folderPath, $"{name}.json");
        return await Task.Run(() => File.Exists(fullPath));
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        Log.Information("Loading all {ConfigType} configurations from {FolderPath}", typeof(T).Name, _folderPath);

        var files = Directory.GetFiles(_folderPath, "*.json");
        var configs = new List<T>();

        foreach (var file in files)
        {
            var config = await _fileManager.LoadAsync<T>(file);
            if (config != null)
            {
                configs.Add(config);
            }
            else
            {
                Log.Warning("Skipped loading configuration from file '{File}' because it returned null.", file);
            }
        }

        Log.Information("Successfully loaded {Count} {ConfigType} configurations.", configs.Count, typeof(T).Name);
        return configs;
    }

    public async Task SaveAsync(T config)
    {
        dynamic dynamicConfig = config; 
        string fileName = $"{dynamicConfig.Name}.json";
        string fullPath = Path.Combine(_folderPath, fileName);

        Log.Information("Saving {ConfigType} configuration: {ConfigName}", typeof(T).Name, dynamicConfig.Name);
        await _fileManager.SaveAsync(config, fullPath);
    }

    public async Task DeleteAsync(string name)
    {
        string fullPath = Path.Combine(_folderPath, $"{name}.json");

        Log.Information("Attempting to delete {ConfigType} configuration: {ConfigName}", typeof(T).Name, name);

        if (File.Exists(fullPath))
        {
            try
            {
                await Task.Run(() => File.Delete(fullPath));
                Log.Information("Successfully deleted configuration: {ConfigName}", name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete configuration file: {FullPath}", fullPath);
                throw;
            }
        }
        else
        {
            Log.Warning("Tried to delete configuration '{ConfigName}', but the file was not found.", name);
        }
    }

    public Task<T?> GetByNameAsync(string name)
    {
        string fullPath = Path.Combine(_folderPath, $"{name}.json");
        Log.Debug("Fetching configuration by name: {ConfigName}", name);

        return _fileManager.LoadAsync<T>(fullPath);
    }

    public async Task RenameAndSaveAsync(string originalName, T config)
    {
        await DeleteAsync(originalName);
        await SaveAsync(config);
    }
}
