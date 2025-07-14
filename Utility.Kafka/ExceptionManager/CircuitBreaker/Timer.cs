using Microsoft.Extensions.Options;
using Utility.Kafka.Abstraction.Errors;
using Utility.Kafka.DependencyInjection;

namespace Utility.Kafka.ExceptionManager.CircuitBreaker;

public class Timer : ICircuitBreakerTimer
{
	private System.Timers.Timer _timer;
	private int _time;
	private bool _disposedValue;
	private TaskCompletionSource<bool>? _tcs;
	private ErrorManagerMiddleware _errorManagerMiddleware;
	private Action? _action;

	public Timer(IOptions<KafkaTimerOptions> options, ErrorManagerMiddleware errorManagerMiddleware)
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		var opts = options.Value;
		_time = opts.ResetTimeout;
		_timer = new System.Timers.Timer(_time)
		{
			AutoReset = false
		};
		_timer.Elapsed += (sender, e) => ElapsedTimerBehavior();

	}

	public void SetTimerConfig(Action action, TaskCompletionSource<bool>? tcs = null)
	{
		_tcs = tcs;
		_action = action;
	}

	private void ElapsedTimerBehavior()
	{
		if(_action == null)
			throw new InvalidOperationException("Action must be set before the timer starts.");
		_action?.Invoke();
		StopTimer();
		if (_tcs != null)
			_tcs.TrySetResult(true);

	}


	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_errorManagerMiddleware.Invoke(() =>
				{
					_timer.Stop();
					_timer.Dispose();
					_tcs = null;
					_action = null;
				}, ErrorManagerMiddleware.OperationClient.Timer, "Dispose");
				
			}
		}
		_disposedValue = true;
	}

	public void StartTimer()
	{
		_timer.Interval = _time; 
		_timer.Start();

	}

	public void StopTimer()
	{
		_timer.Stop();
	}



}
