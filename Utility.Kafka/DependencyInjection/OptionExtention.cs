namespace Utility.Kafka.DependencyInjection;

public class KafkaOptions
{
	public const string SectionName = "Kafka";
	public bool Enable { get; set; } = true;
}

public abstract class KafkaClientOptions
{
	public string BootstrapServers { get; set; } = string.Empty;

}



public class KafkaAdminClientOptions : KafkaClientOptions
{
	public const string SectionName = "Kafka:AdminClient";
}

public class KafkaProducerClientOptions : KafkaClientOptions
{
	public const string SectionName = "Kafka:ProducerClient";
}

public class KafkaConsumerClientOptions : KafkaClientOptions
{
	public const string SectionName = "Kafka:ConsumerClient";

	public string GroupId { get; set; } = string.Empty;
}

public class KafkaCircuitBreakerOptions
{
	public const string SectionName = "Kafka:ProducerService:CircuitBreaker";
	public int MaxFailuresCloseCircuit { get; set; } = 5;
	public int MaxFailuresHalfCloseCircuit { get; set; } = 1;
	public int MaxOpenCircuitCount { get; set; } = 3;
	public int ProbabilityOfFailure { get; set; } = 50;

}

public class KafkaTimerOptions
{
	public const string SectionName = "Kafka:ProducerService:Timer";
	public int ResetTimeout { get; set; } = 3000;
}

public interface IKafkaTopics
{
	IEnumerable<string> GetTopics();
}

public abstract class AbstractInputKafkaTopics : IKafkaTopics
{
	public const string SectionName = "Kafka:Topics:Input";
	public abstract IEnumerable<string> GetTopics();
}

public abstract class AbstractOutputKafkaTopics : IKafkaTopics
{
	public const string SectionName = "Kafka:Topics:Output";
	public abstract IEnumerable<string> GetTopics();
}