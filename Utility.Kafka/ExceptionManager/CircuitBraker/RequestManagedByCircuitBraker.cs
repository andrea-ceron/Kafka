using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Kafka.ExceptionManager.CircuitBraker;

public class RequestManagedByCircuitBraker 
{
	private readonly CircuitBraker _circuitBraker;
	public RequestManagedByCircuitBraker(CircuitBraker circuitBraker)
	{
		_circuitBraker = circuitBraker;
	}
	public async Task Execute(Func<Task> asyncFunc)
	{
		try
		{
			_circuitBraker.Reset();
		}
		catch (Exception ex)
		{
			_circuitBraker.RecordFailure();
			throw new Exception("Error in executing request managed by circuit breaker", ex);
		}
	}
}

