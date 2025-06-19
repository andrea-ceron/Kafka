using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Utility.Kafka.Abstraction.Clients;
using Utility.Kafka.DependencyInjection;

namespace Utility.Kafka.Clients;

public class AdministratorClient : IAdministratorClient
{
	bool _disposedValue;
	readonly IAdminClient _adminClient;

	public AdministratorClient(IOptions<KafkaAdminClientOptions> configOptions)
	{
		_adminClient = ConfigAndBuildAdminClient(configOptions);
	}
	private IAdminClient ConfigAndBuildAdminClient(IOptions<KafkaAdminClientOptions> configOptions)
	{
		var AdminClientBuilder = new AdminClientBuilder(new AdminClientConfig
		{
			BootstrapServers = configOptions.Value.BootstrapServers,
			ClientId = Dns.GetHostName() + "-AdminClient",
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
		await _adminClient.CreateTopicsAsync(new List<TopicSpecification> { TopicConfig });
	}

	public async  Task DeleteTopicAsync(string topicName)
	{
		await _adminClient.DeleteTopicsAsync(new List<string> { topicName });
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
				try
				{
					_adminClient.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error disposing IAdminClient: {ex.Message}");
				}
			}
		}
		_disposedValue = true;
	}
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
