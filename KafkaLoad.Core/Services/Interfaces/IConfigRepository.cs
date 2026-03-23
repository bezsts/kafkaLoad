using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Services.Interfaces;

public interface IConfigRepository<T>
{
    Task<IEnumerable<T>> GetAllAsync();

    Task SaveAsync(T config);

    Task<T?> GetByNameAsync(string name);

    Task DeleteAsync(string name);

    Task<bool> ExistsAsync(string name);
}