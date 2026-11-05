using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Messaging
{
    /// <summary>
    /// Opt-in Kafka produce/consume helper. No-op unless Messaging:Kafka:BootstrapServers is set.
    /// </summary>
    public sealed class KafkaMessageClient : IDisposable
    {
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private IProducer<string, string>? _producer;

        public KafkaMessageClient(string? bootstrapServers = null, string? topic = null)
        {
            _bootstrapServers = bootstrapServers ?? TestConfiguration.Instance.KafkaBootstrapServers;
            _topic = topic ?? TestConfiguration.Instance.KafkaTopic;
        }

        public static bool IsConfigured(string? bootstrapServers = null)
        {
            return !string.IsNullOrWhiteSpace(bootstrapServers ?? TestConfiguration.Instance.KafkaBootstrapServers);
        }

        public async Task ProduceAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            _producer ??= new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            }).Build();

            Logger.Information("Producing Kafka message to {Topic}", _topic);
            await _producer.ProduceAsync(_topic, new Message<string, string> { Key = key, Value = value }, cancellationToken);
        }

        public ConsumeResult<string, string> Consume(string groupId, TimeSpan timeout)
        {
            EnsureConfigured();
            using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).Build();

            consumer.Subscribe(_topic);
            return consumer.Consume(timeout);
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(5));
            _producer?.Dispose();
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_bootstrapServers))
            {
                throw new InvalidOperationException("Kafka is not configured. Set Messaging:Kafka:BootstrapServers.");
            }
        }
    }
}
