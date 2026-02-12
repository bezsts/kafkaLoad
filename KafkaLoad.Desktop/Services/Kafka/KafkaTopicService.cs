using Confluent.Kafka;
using KafkaLoad.Desktop.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.Services.Kafka
{
    public class KafkaTopicService : IKafkaTopicService
    {
        public async Task<bool> TopicExistsAsync(string bootstrapServers, string topicName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var config = new AdminClientConfig { BootstrapServers = bootstrapServers };

                    using var adminClient = new AdminClientBuilder(config).Build();

                    // Fetch metadata for the specific topic with a short timeout
                    var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(3));

                    var topic = metadata.Topics.FirstOrDefault(t => t.Topic == topicName);

                    return topic != null && topic.Error.Code == ErrorCode.NoError;
                }
                catch (Exception)
                {
                    // Connection error or topic not found
                    return false;
                }
            });
        }
    }
}
