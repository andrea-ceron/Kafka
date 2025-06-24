namespace Utility.Kafka.Abstraction.Clients;

public interface IProducerClient<Key, Value> : IDisposable
{
	Task ProduceAsync(string topic,  Key key, Value message, int? partition = null, CancellationToken cancellationToken = default);

}
