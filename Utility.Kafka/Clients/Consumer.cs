using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.DependencyInjection;
using Utility.Kafka.ExceptionManager;

namespace Utility.Kafka.Clients;

public class Consumer : IConsumerClient<string, string>
{
	readonly IConsumer<string, string> _consumer;
	readonly ILogger<Consumer> _logger;
	bool _disposedValue;
	ErrorManagerMiddleware _errorManager;
	public Consumer(IOptions<KafkaConsumerClientOptions> options, ILogger<Consumer> logger, ErrorManagerMiddleware errorManager)
	{
		_logger = logger;
		_consumer = new ConsumerBuilder<string, string>(GetConsumerConfig(options)).Build();
		_errorManager = errorManager;
	}


	private ConsumerConfig GetConsumerConfig(IOptions<KafkaConsumerClientOptions> options)
	{
		ConsumerConfig consumerConfig = new();
		consumerConfig.BootstrapServers = options.Value.BootstrapServers;
		consumerConfig.GroupId = options.Value.GroupId;
		consumerConfig.ClientId = Dns.GetHostName();
		consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
		consumerConfig.EnableAutoCommit = false;
		consumerConfig.AutoCommitIntervalMs = 0;
		consumerConfig.AllowAutoCreateTopics = false;
		_logger.LogInformation("Kafka Consumer Client creato con configurazioni: {configOptions}", JsonSerializer.Serialize(options));
		//consumerConfig.EnableAutoOffsetStore = false;


		return consumerConfig;
	}
	public void Commit(ConsumeResult<string, string>? result)
	{
		if (result != null)
		{
			_errorManager.Invoke(() =>
			{
				_consumer.Commit(result);
			}, ErrorManagerMiddleware.OperationClient.Consume, "Commit");
		}

	}

	public Task<ConsumeResult<string, string>> ConsumeAsync(CancellationToken cancellationToken)
	{
		return  Task.Run(() =>
		{
			ConsumeResult<string, string> result;

			try
			{
				return _consumer.Consume(cancellationToken);
			}
			catch (KafkaException ex)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw;
			}
			return result;
		}, cancellationToken);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_errorManager.Invoke(() =>
				{
					_consumer?.Dispose();
					_consumer?.Close();
				}, ErrorManagerMiddleware.OperationClient.Consume, "Dispose");
			}

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Subscribe(IEnumerable<string> topics)
	{
		_errorManager.Invoke(() =>
		{
			_consumer.Subscribe(topics);
		}, ErrorManagerMiddleware.OperationClient.Consume, "Subscribe");
	}

	public void Subscribe(string topics)
	{
		Subscribe( [topics] );
	}


	public void Unsubscribe()
	{
		_errorManager.Invoke(() =>
		{
			_consumer.Unsubscribe();
		}, ErrorManagerMiddleware.OperationClient.Consume, "Unsubscribe");

	}



	public async Task<bool> ConsumeInLoopAsync(string topic, Func<ConsumeResult<string, string>, Task> comsumerOperationsAsync, CancellationToken cancellationToken = default)
	{
		return await ConsumeInLoopAsync(new List<string>() { topic }, comsumerOperationsAsync, cancellationToken);
	}



	public async Task<bool> ConsumeInLoopAsync(IEnumerable<string> topics, Func<ConsumeResult<string, string>, Task> comsumerOperationsAsync, CancellationToken cancellationToken = default)

	{
		Subscribe(topics);
		ConsumeResult<string, string>? result = null;

		while(cancellationToken.IsCancellationRequested == false)
		{

			await _errorManager.InvokeAsync(async () =>
			{
				result = await ConsumeAsync(cancellationToken);
				if (result != null)
				{
					await comsumerOperationsAsync(result);
				}
			}, ErrorManagerMiddleware.OperationClient.Consume, "ConsumeInLoopAsync");
		
			Commit(result);

		}
		return true;
	}
}

