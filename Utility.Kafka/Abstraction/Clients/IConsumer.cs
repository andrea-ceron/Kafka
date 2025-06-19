using Confluent.Kafka;

namespace Utility.Kafka.Abstraction.Clients;

public interface IConsumer<Key, Value> : IDisposable
{
	void Subscribe(IEnumerable<string> topics);
	void UnsubscribeAll();
	void Unsubscribe(string topic);
	Task<ConsumeResult<Key, Value>> ConsumeAsync(CancellationToken cancellationToken);
	void Commit(ConsumeResult<Key, Value>? result);


}
