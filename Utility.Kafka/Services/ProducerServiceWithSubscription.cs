using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.ExceptionManager;

namespace Utility.Kafka.Services;

public abstract class ProducerServiceWithSubscription(IServiceProvider serviceProvider, ErrorManagerMiddleware errorManager) : BackgroundService
{
	private ErrorManagerMiddleware _errorManager = errorManager;
	protected abstract IDisposable Subscribe(TaskCompletionSource<bool> tcs);
	protected abstract IEnumerable<string> GetTopics();
	protected abstract Task OperationsAsync(CancellationToken cancellationToken);
	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		IAdministratorClient adminClient = serviceProvider.GetRequiredService<IAdministratorClient>();
		foreach (var topic in GetTopics())
		{
			if (! adminClient.TopicExist(topic))
			{
				await adminClient.CreateTopicAsync(topic);
			}
		}
		await _errorManager.InvokeAsync(async () =>
		{
			await base.StartAsync(cancellationToken);
		}, ErrorManagerMiddleware.OperationClient.ProducerService, "StartAsync");
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var tcs = new TaskCompletionSource<bool>();
			await _errorManager.InvokeAsync(async () =>
			{
				using var subscription = Subscribe(tcs);
				await OperationsAsync(stoppingToken);
				await tcs.Task;
			}, ErrorManagerMiddleware.OperationClient.ProducerService, "ExecuteAsync");
		}

	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await _errorManager.InvokeAsync(async () =>
		{
			await base.StopAsync(cancellationToken);
		}, ErrorManagerMiddleware.OperationClient.ProducerService, "StopAsync");
	}



}
