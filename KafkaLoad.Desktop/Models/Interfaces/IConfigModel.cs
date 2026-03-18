using KafkaLoad.Desktop.Models.Configs;

namespace KafkaLoad.Desktop.Models.Interfaces
{
    public interface IConfigModel
    {
        string Name { get; set; }

        //CustomSecurityConfig Security { get; set; }
    }
}