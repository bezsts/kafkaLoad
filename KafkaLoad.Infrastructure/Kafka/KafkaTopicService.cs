using Confluent.Kafka;
using KafkaLoad.Core.Services.Interfaces;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KafkaLoad.Infrastructure.Kafka
{
    public class KafkaTopicService : IKafkaTopicService
    {
        public async Task<bool> TopicExistsAsync(string bootstrapServers, string topicName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Log.Debug("Checking if topic '{TopicName}' exists on brokers: {Brokers}", topicName, bootstrapServers);

                    var config = new AdminClientConfig { BootstrapServers = bootstrapServers };

                    using var adminClient = new AdminClientBuilder(config).Build();

                    // Fetch metadata for the specific topic with a short timeout
                    var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(3));

                    var topic = metadata.Topics.FirstOrDefault(t => t.Topic == topicName);

                    bool exists = topic != null && topic.Error.Code == ErrorCode.NoError;

                    if (!exists)
                    {
                        Log.Warning("Topic '{TopicName}' does not exist or returned an error. Kafka Error Code: {ErrorCode}", topicName, topic?.Error.Code);
                    }

                    return exists;
                }
                catch (Exception ex)
                {
                    // Connection error or topic not found
                    Log.Error(ex, "Failed to fetch metadata for topic '{TopicName}' from brokers: {Brokers}", topicName, bootstrapServers);
                    return false;
                }
            });
        }
    }
}
