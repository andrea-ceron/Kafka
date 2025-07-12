namespace Utility.Kafka.ExceptionManager.CircuitBraker;

public class CircuitBraker
{
	public enum CircuitState { Close, Open, HalfOpen }
	private readonly int _maxFailuresCloseCircuit;
	private readonly int _maxFailuresHalfCloseCircuit;

	private int _failureCount;
	private CircuitState _state;
	private bool _disposedValue;
	private int _openCircuitCount;
	private int _maxOpenCircuitCount;
	private TaskCompletionSource<bool> _tcs;
	private ErrorManagerMiddleware _errorManagerMiddleware;

	public CircuitBraker(int maxFailuresCloseCircuit, int maxFailuresHalfCloseCircuit, TimeSpan resetTimeout, int maxOpenCircuitCount, ErrorManagerMiddleware errorManagerMiddleware)
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		_maxFailuresCloseCircuit = maxFailuresCloseCircuit;
		_maxFailuresHalfCloseCircuit = maxFailuresHalfCloseCircuit;
		_failureCount = 0;
		_state = CircuitState.Close;
		_maxOpenCircuitCount = maxOpenCircuitCount;
		_tcs = new();
	}

	public CircuitState ReturnCircuitState()
	{
		 return _state; 
	}
	public void OpenCircuit()
	{
		_state = CircuitState.Open;
		++_openCircuitCount;
		_tcs = new();
		CircuitResetTimer timer = new(3000, HalfOpenCircuit, _tcs, _errorManagerMiddleware); 
		timer.StartTimer();
	}

	public void HalfOpenCircuit()
	{
		_state = CircuitState.HalfOpen;
		CircuitResetTimer timer = new(3000, CloseCircuit, null, _errorManagerMiddleware);
		timer.StartTimer();
	}

	public void CloseCircuit()
	{
		_state = CircuitState.Close;
		Reset();
	}
	public void RecordFailure()
	{
		_failureCount++;
		if(_state == CircuitState.Close && _failureCount >= _maxFailuresCloseCircuit)
		{
			Reset();
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



	public async  Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
	{
		while (_openCircuitCount <_maxOpenCircuitCount)
		{

			var (shouldContinue, res) = await _errorManagerMiddleware.InvokeAsync(async () =>
			{
				if (_state == CircuitState.Open)
				{
					await _tcs.Task.WaitAsync(ct);

				}

				return await action.Invoke();
			}, ErrorManagerMiddleware.OperationClient.CircuitBreaker, "ExecuteAsync");
			if(shouldContinue)
			{
				RecordFailure();
				continue;
			}
			return res;


		}
		throw new InvalidOperationException("CircuitBreaker si è aperto troppe volte");
	}


}
