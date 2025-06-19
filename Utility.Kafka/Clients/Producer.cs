using Confluent.Kafka;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.ExceptionManager.CircuitBraker;
namespace Utility.Kafka.Clients;

public class Producer : IProducerClient<string, string>
{
	bool _disposedValue;
	readonly IProducer<string, string> _producer;

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_producer.Flush();
				_producer.Dispose();
			}
		}
		_disposedValue = true;
	}

	public async Task ProduceAsync(string topic, int? partition = null, string key, string message, CancellationToken cancellationToken = default)
	{
		await ProduceAsyncCircuitBraker(topic, partition, key, message, cancellationToken);
	}

	private async Task ProduceAsyncCircuitBraker(string topic, int? partition, string key, string message, CancellationToken cancellationToken = default)
	{
		DeliveryResult<string, string>? deliveryResult;
		CircuitBraker circuitBraker = new(10, 5, TimeSpan.FromSeconds(30));
		Message<string, string> msg = new() { Key = key, Value = message };
		try
		{
			if (partition.HasValue)
			{
				TopicPartition topicPartition = new(topic, partition.Value);
				var res  = new RequestManagedByCircuitBraker(circuitBraker);
				//, await _producer.ProduceAsync(topicPartition, msg, cancellationToken)
			}
			else
			{
				deliveryResult = await _producer.ProduceAsync(topic, msg, cancellationToken);
			}

		}catch(Exception ex)
		{
			circuitBraker.RecordFailure();
			throw new Exception("Error in producing message", ex);
		}

	}
}
