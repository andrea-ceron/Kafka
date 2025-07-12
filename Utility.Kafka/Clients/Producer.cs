using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
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
	ILogger<Producer> _logger;
	private readonly double ErrorTestCeilingProbability; // Probabilità di errore per testare il Circuit Braker

	public Producer(
		IOptions<KafkaProducerClientOptions> producerOptions,
		IOptions<KafkaProducerServiceOptions> circuitBreakerOptions,
		ILogger<Producer> logger,
		ErrorManagerMiddleware errorManagerMiddleware)
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		_producer = new ProducerBuilder<string, string>(GetProducerConfig(producerOptions)).Build();
		_logger = logger;
		_circuitBraker = new CircuitBraker(
			maxFailuresCloseCircuit: circuitBreakerOptions.Value.MaxFailuresCloseCircuit,
			maxFailuresHalfCloseCircuit: circuitBreakerOptions.Value.MaxFailuresHalfCloseCircuit,
			resetTimeout: TimeSpan.FromMilliseconds(circuitBreakerOptions.Value.ResetTimeout),
			maxOpenCircuitCount: circuitBreakerOptions.Value.MaxOpenCircuitCount,
			errorManagerMiddleware: errorManagerMiddleware
		);
		ErrorTestCeilingProbability = (double) circuitBreakerOptions.Value.ProbabilityOfFailure /100;
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
		var randomValue = new Random();
		return await _circuitBraker.ExecuteAsync(async () =>
		{
			var elem = randomValue.NextDouble();
			_logger.LogWarning($"Esecuzione Action da ProduceAsyncCircuitBreaker, random {elem}, ceiling {ErrorTestCeilingProbability}");

			if (elem < ErrorTestCeilingProbability)
			{
				_logger.LogWarning($"Simulazione di errore per testare il Circuit Braker, random {elem}, ceiling {ErrorTestCeilingProbability}");
				throw new KafkaException(new Error(ErrorCode.InvalidRequest, $"Simulazione di errore per testare il Circuit Braker, random {elem}, ceiling {ErrorTestCeilingProbability}"));
			}
			else if (partition.HasValue)
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
