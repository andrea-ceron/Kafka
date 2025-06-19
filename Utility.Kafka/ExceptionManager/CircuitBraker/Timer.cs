using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Utility.Kafka.ExceptionManager.CircuitBraker;

public class CircuitResetTimer : IDisposable
{
	private System.Timers.Timer _timer;
	private int _time;
	private bool _disposedValue;

	public CircuitResetTimer(int time, Action action )
	{
		_time = time;
		_timer = new System.Timers.Timer(time);
		_timer.AutoReset = false;
		_timer.Elapsed += (sender, e) =>
		{
			action.Invoke();
			StopTimer();
		};


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
				_timer.Dispose();
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
