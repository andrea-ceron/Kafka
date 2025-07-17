using Confluent.Kafka;
using Microsoft.Extensions.Logging;
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
	private ErrorManagerMiddleware _errorManagerMiddleware;
	private double _errorTestCeilingProbability;
	private ICircuitBreakerTimer _timer;
	private ILogger _logger;
	private SemaphoreSlim _semaphore;
	private CancellationTokenSource? _cancellationTokenSource;
	private int _successesWhileInHalfOpenCircuit;

	public CircuitBreaker(IOptions<KafkaCircuitBreakerOptions> options, ErrorManagerMiddleware errorManagerMiddleware, ICircuitBreakerTimer timer, ILogger logger, SemaphoreSlim semaphore )
	{
		_errorManagerMiddleware = errorManagerMiddleware;
		var opts = options.Value;
		_errorTestCeilingProbability = (double) opts.ProbabilityOfFailure /100; 
		_maxFailuresWhenCloseCircuit = opts.MaxFailuresCloseCircuit;
		_maxFailuresWhenHalfCloseCircuit = opts.MaxFailuresHalfCloseCircuit;
		_maxOpenCircuitCount = opts.MaxOpenCircuitCount;
		_failedExecutionCounter = 0;
		_state = CircuitState.Close;
		_timer = timer;
		_logger = logger;
		_semaphore = semaphore;
		_successesWhileInHalfOpenCircuit = 0;
	}

	public CircuitState ReturnCircuitState()
	{
		 return _state; 
	}
	private async Task TransitionToOpenCircuit(CancellationToken ct = default)
	{
		_logger.LogInformation("Apertura Stato ");
		_state = CircuitState.Open;
		++_openCircuitCount;
		_logger.LogInformation("Configurazione Timer ");
		await _timer.OpenCircuitWait();
		_logger.LogInformation("Eseguita OpenCircuit ");

		//await _semaphore.WaitAsync(ct);
	}
	private async Task TransitionToHalfOpenCircuit(CancellationToken ct = default)
	{
		_state = CircuitState.HalfOpen;
		_logger.LogInformation("Transizione dallo stato Open a HalfOpen, stato attuale: {state}", _state);
		//var cts = new CancellationTokenSource();
		//_cancellationTokenSource = cts;
		//await _timer.HalfOpenCircuitWait(TransitionToCloseCircuit, ct);
	}
	private void TransitionToCloseCircuit()
	{
		_logger.LogInformation("Chiusura Stato");
		_state = CircuitState.Close;
		Reset();
	}
	private async Task RecordFailure()
	{
		_logger.LogInformation("Registrazione fallimento, stato attuale: {state}", _state);
		_failedExecutionCounter++;
		if(_state == CircuitState.Close && _failedExecutionCounter >= _maxFailuresWhenCloseCircuit)
		{
			_logger.LogInformation("Circuito chiuso, superato il numero massimo di fallimenti: {maxFailures}", _maxFailuresWhenCloseCircuit);
			Reset();
			await TransitionToOpenCircuit();
		}
		if (_state == CircuitState.HalfOpen && _failedExecutionCounter >= _maxFailuresWhenHalfCloseCircuit)
		{
			_logger.LogInformation("Circuito half-open, superato il numero massimo di fallimenti: {maxFailures}", _maxFailuresWhenHalfCloseCircuit);
			Reset();
			await TransitionToOpenCircuit();
		}
	}
	private void Reset()
	{
		_failedExecutionCounter = 0;
		_successesWhileInHalfOpenCircuit = 0;
	}
	public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
	{
		_logger.LogInformation("Entrato in ExecuteAsync con CircuitBreaker, stato attuale: {state}", _state);
		while (_openCircuitCount < _maxOpenCircuitCount)
		{
			var compareValue = Random.Shared.NextDouble();
			var (shouldContinue, res) = await _errorManagerMiddleware.InvokeAsync(async () =>
			{
				if(_state == CircuitState.HalfOpen && _successesWhileInHalfOpenCircuit == 2)
					TransitionToCloseCircuit();
				if (_state == CircuitState.Open)
					await TransitionToHalfOpenCircuit();
				if (compareValue < _errorTestCeilingProbability)
					throw new KafkaException(new Error(ErrorCode.InvalidRequest, $"Simulazione di errore per testare il Circuit Braker, random {compareValue}, ceiling {_errorTestCeilingProbability}"));
				_logger.LogInformation("Esecuzione dell'action, stato attuale: {state}", _state);
				if(_state == CircuitState.HalfOpen)
					_successesWhileInHalfOpenCircuit++;

				return await action.Invoke();
			}, ErrorManagerMiddleware.OperationClient.CircuitBreaker, "ExecuteAsync");
			if (shouldContinue)
			{
				_logger.LogInformation("CircuitBreaker ha registrato un fallimento, stato attuale: {state}", _state);
				await RecordFailure();
				continue;
			}
			if (res is null)
				throw new InvalidOperationException("Il risultato dell'action è null.");
			return res;
		}
		_openCircuitCount = 0;
		throw new InvalidOperationException("CircuitBreaker si è aperto troppe volte");
	}
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	protected void Dispose(bool disposing)
	{
		if (!_disposedValue && disposing)
		{

			_semaphore?.Dispose();
		}
		_disposedValue = true;
	}
}
