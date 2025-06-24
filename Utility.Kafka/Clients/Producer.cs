using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.DependencyInjection;
using Utility.Kafka.ExceptionManager;
using Utility.Kafka.ExceptionManager.CircuitBraker;
namespace Utility.Kafka.Clients;

public class Producer : IProducerClient<string, string>
{
	bool _disposedValue;
	readonly IProducer<string, string> _producer;
	private CircuitBraker? _circuitBraker = null;
	ErrorManagerMiddleware _errorManagerMiddleware;

	public Producer(
		IOptions<KafkaProducerClientOptions> producerOptions,
		IOptions<KafkaProducerServiceOptions> circuitBreakerOptions,
		ErrorManagerMiddleware errorManagerMiddleware)
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		_producer = new ProducerBuilder<string, string>(GetProducerConfig(producerOptions)).Build();

		_circuitBraker = new CircuitBraker(
			maxFailuresCloseCircuit: circuitBreakerOptions.Value.MaxFailuresCloseCircuit,
			maxFailuresHalfCloseCircuit: circuitBreakerOptions.Value.MaxFailuresHalfCloseCircuit,
			resetTimeout: TimeSpan.FromMilliseconds(circuitBreakerOptions.Value.ResetTimeout),
			maxOpenCircuitCount: circuitBreakerOptions.Value.MaxOpenCircuitCount,
			errorManagerMiddleware: errorManagerMiddleware
		);
	}

	private ProducerConfig GetProducerConfig(IOptions<KafkaProducerClientOptions> options)
	{
		ProducerConfig producerConfig = new();
		producerConfig.BootstrapServers = options.Value.BootstrapServers;
		producerConfig.ClientId = Dns.GetHostName();
		producerConfig.Acks = Acks.All;
		return producerConfig;
	}
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
				_errorManagerMiddleware.Invoke(() =>
				{
					_producer.Flush();
					_producer.Dispose();
				}, ErrorManagerMiddleware.OperationClient.Produce, "Dispose");
			{

			}
		_disposedValue = true;
		}
	}

	public async Task ProduceAsync(string topic,  string key, string message, int? partition = null, CancellationToken cancellationToken = default)
	{
		DeliveryResult<string, string>? deliveryResult;
		Message<string, string> msg = new() { Key = key, Value = message };

		await _errorManagerMiddleware.InvokeAsync(async () =>
		{
			deliveryResult = await ProduceAsyncCircuitBraker(topic, partition, msg, cancellationToken);
		}, ErrorManagerMiddleware.OperationClient.Produce, "ProduceAsync");
	}

	private async Task<DeliveryResult<string,string>?> ProduceAsyncCircuitBraker(string topic, int? partition, Message<string,string> msg, CancellationToken cancellationToken = default)
	{
		return await _circuitBraker.ExecuteAsync(async () =>
		{
			if (partition.HasValue)
			{
				TopicPartition topicPartition = new(topic, partition.Value);
				return await _producer.ProduceAsync(topicPartition, msg, cancellationToken);
			}
			else
			{
				return await _producer.ProduceAsync(topic, msg, cancellationToken);
			}
		}, cancellationToken);
	}


}
