using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.Abstraction.MessageHandlers;
using Utility.Kafka.DependencyInjection;
using Utility.Kafka.ExceptionManager;

namespace Utility.Kafka.Services;

public class ConsumerService<TKafkaTopicsInput>(IConsumerClient<string, string> consumerClient, 
	IAdministratorClient adminClient, IOptions<TKafkaTopicsInput> options, 
	IServiceProvider service, IMessageHandlerFactory<string, string> messageFactory, ErrorManagerMiddleware errorManagerMiddleware) : BackgroundService
	 where TKafkaTopicsInput : class, IKafkaTopics
{
	bool _disposedValue;
	private readonly IEnumerable<string> _topics = options.Value.GetTopics();
	private ErrorManagerMiddleware _errorManager = errorManagerMiddleware;

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		foreach (var topic in _topics)
		{
			if (!adminClient.TopicExist(topic))
			{
				await adminClient.CreateTopicAsync(topic);
			}
		}
		await _errorManager.InvokeAsync(async () =>
		{
			await base.StartAsync(cancellationToken);
		}, ErrorManagerMiddleware.OperationClient.ConsumerService, "StartAsync");

	}
	protected async override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await consumerClient.ConsumeInLoopAsync(_topics, async msg => {
			await using var scope = service.CreateAsyncScope();
			IMessageHandler<string, string> handler = messageFactory.Create(msg.Topic, scope.ServiceProvider);
			await handler.OnMessageReceivedAsync(msg.Message.Key, msg.Message.Value);
		}, stoppingToken);
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await _errorManager.InvokeAsync(async () =>
		{
			await base.StopAsync(cancellationToken);

		}, ErrorManagerMiddleware.OperationClient.ConsumerService, "StopAsync");
	}
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_errorManager.Invoke(() =>
				{
					consumerClient?.Dispose();
					adminClient?.Dispose();
					base.Dispose();
				}, ErrorManagerMiddleware.OperationClient.ConsumerService, "Dispose");

			}
			_disposedValue = true;
		}
	}
	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
