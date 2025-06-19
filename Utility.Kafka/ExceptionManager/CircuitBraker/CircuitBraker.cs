using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Utility.Kafka.ExceptionManager.CircuitBraker;

public class CircuitBraker
{
	private readonly int _maxFailuresCloseCircuit;
	private readonly int _maxFailuresHalfCloseCircuit;

	private int _failureCount;
	private enum CircuitState { Closed, Open, HalfOpen }
	private CircuitState _state;
	private bool _disposedValue;

	public CircuitBraker(int maxFailuresCloseCircuit, int maxFailuresHalfCloseCircuit, TimeSpan resetTimeout)
	{
		_maxFailuresCloseCircuit = maxFailuresCloseCircuit;
		_maxFailuresHalfCloseCircuit = maxFailuresHalfCloseCircuit;
		_failureCount = 0;
		_state = CircuitState.Closed;
	}
	public void OpenCircuit()
	{
		_state = CircuitState.Open;
		CircuitResetTimer timer = new(3000, HalfCloseCircuit); 
		timer.StartTimer();
	}

	public void HalfCloseCircuit()
	{
		_state = CircuitState.HalfOpen;
		CircuitResetTimer timer = new(3000, CloseCircuit);
		timer.StartTimer();
	}

	public void CloseCircuit()
	{
		_state = CircuitState.Closed;
		Reset();
	}
	public void RecordFailure()
	{
		_failureCount++;
		if(_state == CircuitState.Closed && _failureCount >= _maxFailuresCloseCircuit)
		{
			OpenCircuit();
		}
		if (_state == CircuitState.HalfOpen && _failureCount >= _maxFailuresHalfCloseCircuit)
		{
			Reset();
			OpenCircuit();
		}
	}
	public void Reset()
	{
		_failureCount = 0;
	}

}
