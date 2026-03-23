using KafkaLoad.Core.Models.Configs;

namespace KafkaLoad.Core.Models.Interfaces
{
    public interface IConfigModel
    {
        string Name { get; set; }

        //CustomSecurityConfig Security { get; set; }
    }
}