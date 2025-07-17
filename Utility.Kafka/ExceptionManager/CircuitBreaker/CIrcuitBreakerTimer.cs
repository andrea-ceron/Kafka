using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utility.Kafka.Abstraction.Errors;
using Utility.Kafka.DependencyInjection;

namespace Utility.Kafka.ExceptionManager.CircuitBreaker;

public class CircuitBreakerTimer : ICircuitBreakerTimer
{
	private readonly int _time;
	private int _secondsToWait;
	private bool _disposedValue;
	private ErrorManagerMiddleware _errorManagerMiddleware;
	ILogger _logger;
	private SemaphoreSlim _semaphore;

	public CircuitBreakerTimer(IOptions<KafkaTimerOptions> options, ErrorManagerMiddleware errorManagerMiddleware, ILogger logger, SemaphoreSlim semaphore)
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		var opts = options.Value;
		_time = opts.ResetTimeout;
		_logger = logger;
		_secondsToWait = _time;
		_semaphore = semaphore;
	}
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	public async Task OpenCircuitWait()
	{

		await Task.Delay(_secondsToWait);

	}
	protected void Dispose(bool disposing)
	{
		if (!_disposedValue && disposing)
		{
		}
		_disposedValue = true;
	}
}
