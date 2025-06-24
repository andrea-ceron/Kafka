using Confluent.Kafka;

namespace Utility.Kafka.Abstraction.Clients;

public interface IConsumerClient<Key, Value> : IDisposable
{
	void Subscribe(IEnumerable<string> topics);
	void Unsubscribe();
	Task<ConsumeResult<Key, Value>> ConsumeAsync(CancellationToken cancellationToken);
	void Commit(ConsumeResult<Key, Value>? result);
	public void Subscribe(string topics);
	Task<bool> ConsumeInLoopAsync(string topic, Func<ConsumeResult<Key, Value>, Task> comsumerOperationsAsync, CancellationToken cancellationToken = default);
	Task<bool> ConsumeInLoopAsync(IEnumerable<string> topic, Func<ConsumeResult<Key, Value>, Task> comsumerOperationsAsync, CancellationToken cancellationToken = default);

}