using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Kafka.Abstraction.Clients;

public interface IAdministratorClient : IDisposable
{
	Task CreateTopicAsync(string topicName, int numPartitions = 1, short replicationFactor = 1);
	Task DeleteTopicAsync(string topicName);
	bool TopicExist(string topicName);
	Metadata GetKafkaClusterData(string? topic = null);

}
