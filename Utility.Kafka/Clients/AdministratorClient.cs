using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.Abstraction.Errors;
using Utility.Kafka.DependencyInjection;
using Utility.Kafka.ExceptionManager;

namespace Utility.Kafka.Clients;

public class AdministratorClient : IAdministratorClient
{
	bool _disposedValue;
	readonly IAdminClient _adminClient;
	readonly ILogger<AdministratorClient> _logger;
	ErrorManagerMiddleware _errorManagerMiddleware;


	public AdministratorClient(IOptions<KafkaAdminClientOptions> configOptions, ILogger<AdministratorClient> logger, ErrorManagerMiddleware errorManager)
	{
		_errorManagerMiddleware = errorManager;
		_logger = logger;
		_adminClient = ConfigAndBuildAdminClient(configOptions);
		_logger.LogInformation("Kafka Administrator Client creato con configurazioni: {configOptions}", JsonSerializer.Serialize(configOptions));
	}
	private IAdminClient ConfigAndBuildAdminClient(IOptions<KafkaAdminClientOptions> configOptions)
	{
		var AdminClientBuilder = new AdminClientBuilder(new AdminClientConfig
		{
			BootstrapServers = configOptions.Value.BootstrapServers,
			ClientId = Dns.GetHostName() 
		});
		return AdminClientBuilder.Build();
	}

	public async  Task CreateTopicAsync(string topicName, int numPartitions = 1, short replicationFactor = 1)
	{
		// Aggiungere gesetione errori
		var TopicConfig = new TopicSpecification
		{
			Name = topicName,
			NumPartitions = numPartitions,
			ReplicationFactor = replicationFactor,
		};

		await _errorManagerMiddleware.InvokeAsync(async () =>
	   {
		   await _adminClient.CreateTopicsAsync(new List<TopicSpecification> { TopicConfig });
	   },  ErrorManagerMiddleware.OperationClient.Admin, nameof(CreateTopicAsync));
		_logger.LogInformation("Topic creato: {topicName}", topicName);

	}

	public async  Task DeleteTopicAsync(string topicName)
	{
		await _errorManagerMiddleware.InvokeAsync(async () =>
		{
			await _adminClient.DeleteTopicsAsync(new List<string> { topicName });
		}, ErrorManagerMiddleware.OperationClient.Admin, nameof(DeleteTopicAsync));
		_logger.LogInformation("Topic eliminato: {topicName}", topicName);
	}

	public Metadata GetKafkaClusterData(string? topic = null)
	{
		// ricordarsi di disabilitare il auto.createw.topics.enable quando si vuole ottenere i metadati di tutto il cluster
		if (string.IsNullOrEmpty(topic))
		{
			return _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
		}
		return _adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
	}

	public bool TopicExist(string topicName)
	{
		return GetKafkaClusterData(topicName).Topics.Count != 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				 _errorManagerMiddleware.Invoke( () =>
				{
					_adminClient.Dispose();
				}, ErrorManagerMiddleware.OperationClient.Admin, nameof(Dispose));
			}
		_disposedValue = true;
		}
	}
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
