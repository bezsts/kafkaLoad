using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Interfaces;

public interface IConfigRepository<T>
{
    Task<IEnumerable<T>> GetAllAsync();

    Task SaveAsync(T config);

    Task<T?> GetByNameAsync(string name);
    
    Task DeleteAsync(string name);
}