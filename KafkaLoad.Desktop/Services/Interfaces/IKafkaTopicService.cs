using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Interfaces
{
    public interface IKafkaTopicService
    {
        /// <summary>
        /// Checks if a specific topic exists on the cluster.
        /// </summary>
        Task<bool> TopicExistsAsync(string bootstrapServers, string topicName);
    }
}
