namespace Utility.Kafka.Abstraction.Errors;

public interface ICircuitBreakerTimer
{
	Task OpenCircuitWait();
	void Dispose();
}
