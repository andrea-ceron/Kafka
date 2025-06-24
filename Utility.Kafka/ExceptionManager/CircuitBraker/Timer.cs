using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static System.Collections.Specialized.BitVector32;

namespace Utility.Kafka.ExceptionManager.CircuitBraker;

public class CircuitResetTimer : IDisposable
{
	private System.Timers.Timer _timer;
	private int _time;
	private bool _disposedValue;
	private TaskCompletionSource<bool>? _tcs;
	private CircuitBraker.CircuitState _state;
	private ErrorManagerMiddleware _errorManagerMiddleware;

	public CircuitResetTimer(int time, Action action, TaskCompletionSource<bool>? tcs, ErrorManagerMiddleware errorManagerMiddleware)
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		_time = time;
		_timer = new System.Timers.Timer(time);
		_timer.AutoReset = false;
		_tcs = tcs;

		_timer.Elapsed += (sender, e) => ElapsedTimerBehavior(action);
	}

	private void ElapsedTimerBehavior(Action action)
	{

		action.Invoke();
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
					_timer.Dispose();
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
		Dispose();
	}


}
