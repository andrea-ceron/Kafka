namespace Utility.Kafka.Abstraction.Errors;

public interface ICircuitBreakerTimer
{
	void SetTimerConfig(Action action, TaskCompletionSource<bool>? tcs = null);
	void StartTimer();
	void StopTimer();
	void Dispose();
}
