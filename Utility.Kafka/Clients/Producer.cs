using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.Abstraction.Errors;
using Utility.Kafka.DependencyInjection;
using Utility.Kafka.ExceptionManager;
namespace Utility.Kafka.Clients;

public class Producer : IProducerClient<string, string>
{
	bool _disposedValue;
	readonly IProducer<string, string> _producer;
	private ICircuitBreaker _circuitBreaker;
	ErrorManagerMiddleware _errorManagerMiddleware;
	ILogger<Producer> _logger;

	public Producer(
		IOptions<KafkaProducerClientOptions> producerOptions,
		ILogger<Producer> logger,
		ErrorManagerMiddleware errorManagerMiddleware,
		ICircuitBreaker circuitBreaker
		)
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		_producer = new ProducerBuilder<string, string>(GetProducerConfig(producerOptions)).Build();
		_logger = logger;
		_circuitBreaker = circuitBreaker;

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
		_logger.LogInformation("Entrato in ProduceAsync");
		DeliveryResult<string, string>? deliveryResult = null;
		Message<string, string> msg = new() { Key = key, Value = message };
		await ProduceAsyncCircuitBraker(topic, partition, msg, cancellationToken);
	}

	private async Task<DeliveryResult<string,string>?> ProduceAsyncCircuitBraker(string topic, int? partition, Message<string,string> msg, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Entrato in ProduceAsyncCircuitBraker");
		return await _circuitBreaker.ExecuteAsync(async () =>
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
		}
	, cancellationToken);
	}




}
