using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Utility.Kafka.Abstraction.Errors;
using Utility.Kafka.DependencyInjection;

namespace Utility.Kafka.ExceptionManager.CircuitBreaker;

public class CircuitBreaker : ICircuitBreaker
{
	public enum CircuitState { Close, Open, HalfOpen }
	private readonly int _maxFailuresWhenCloseCircuit;
	private readonly int _maxFailuresWhenHalfCloseCircuit;

	private int _failedExecutionCounter;
	private CircuitState _state;
	private bool _disposedValue;
	private int _openCircuitCount;
	private int _maxOpenCircuitCount;
	private TaskCompletionSource<bool> _tcs;
	private ErrorManagerMiddleware _errorManagerMiddleware;
	private double _errorTestCeilingProbability;
	private Random randomValue = new Random();
	private ICircuitBreakerTimer _timer;

	public CircuitBreaker(IOptions<KafkaCircuitBreakerOptions> options, ErrorManagerMiddleware errorManagerMiddleware, ICircuitBreakerTimer timer )
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		var opts = options.Value;
		_errorTestCeilingProbability = (double) opts.ProbabilityOfFailure /100; 
		_maxFailuresWhenCloseCircuit = opts.MaxFailuresCloseCircuit;
		_maxFailuresWhenHalfCloseCircuit = opts.MaxFailuresHalfCloseCircuit;
		_maxOpenCircuitCount = opts.MaxOpenCircuitCount;
		_failedExecutionCounter = 0;
		_state = CircuitState.Close;
		_tcs = new();
		_timer = timer;
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
		_timer.SetTimerConfig(() => HalfOpenCircuit(), _tcs);
		_timer.StartTimer();
	}

	public void HalfOpenCircuit()
	{
		_state = CircuitState.HalfOpen;
		_timer.SetTimerConfig(() => CloseCircuit(), null);
		_timer.StartTimer();
	}

	public void CloseCircuit()
	{
		_state = CircuitState.Close;
		Reset();
	}
	public void RecordFailure()
	{
		_failedExecutionCounter++;
		if(_state == CircuitState.Close && _failedExecutionCounter >= _maxFailuresWhenCloseCircuit)
		{
			Reset();
			OpenCircuit();
		}
		if (_state == CircuitState.HalfOpen && _failedExecutionCounter >= _maxFailuresWhenHalfCloseCircuit)
		{
			Reset();
			OpenCircuit();
		}
	}
	public void Reset()
	{
		_failedExecutionCounter = 0;
	}



	public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
	{
		while (_openCircuitCount < _maxOpenCircuitCount)
		{
			var compareValue = randomValue.NextDouble();
			var (shouldContinue, res) = await _errorManagerMiddleware.InvokeAsync(async () =>
			{
				if (_state == CircuitState.Open)
					await _tcs.Task.WaitAsync(ct);
				if (compareValue < _errorTestCeilingProbability)
					throw new KafkaException(new Error(ErrorCode.InvalidRequest, $"Simulazione di errore per testare il Circuit Braker, random {compareValue}, ceiling {_errorTestCeilingProbability}"));
				return await action.Invoke();
			}, ErrorManagerMiddleware.OperationClient.CircuitBreaker, "ExecuteAsync");
			if (shouldContinue)
			{
				RecordFailure();
				continue;
			}
			if (res is null)
				throw new InvalidOperationException("Il risultato dell'action è null.");
			return res;
		}
		throw new InvalidOperationException("CircuitBreaker si è aperto troppe volte");
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
					Dispose();
				}, ErrorManagerMiddleware.OperationClient.Timer, "Dispose");

			}
		}
		_disposedValue = true;
	}



}
