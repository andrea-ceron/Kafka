using Utility.Kafka.ExceptionManager.CircuitBreaker;

namespace Utility.Kafka.Abstraction.Errors;

public interface ICircuitBreaker
{
	Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
	CircuitBreaker.CircuitState ReturnCircuitState();

}
