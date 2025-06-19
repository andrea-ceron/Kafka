namespace Utility.Kafka.Abstraction.Clients;

public interface IProducerClient<Key, Value> : IDisposable
{
	Task ProduceAsync(string topic, int partition, Key key, Value message, CancellationToken cancellationToken = default);

}
