using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utility.Kafka.Abstraction.Errors;

namespace Utility.Kafka.ExceptionManager;

public class ErrorManagerMiddleware(ILogger<ErrorManagerMiddleware> logger) 
{
	private readonly ILogger<ErrorManagerMiddleware> _logger = logger;
	public enum OperationClient
	{
		Unkwown,
		Produce,
		Consume,
		Admin,
		CircuitBreaker,
		 Timer,
		ConsumerService,
		ProducerService,
		OperationMessageHandlerBase
	}
	public async Task InvokeAsync(Func<Task> task, OperationClient ot = OperationClient.Unkwown, string methodName = "Unkwnown")
	{
		try
		{
			 await task.Invoke();
		}
		catch (ConsumeException ex) when (ot == OperationClient.Admin)
		{
			_logger.LogError(ex, "CreateTopicsException sollevata all'interno del metodo {methodName}", methodName);
			return;
		}

		catch (DeleteTopicsException ex) when (ot == OperationClient.Admin)
		{
			_logger.LogError(ex, "DeleteTopicsException sollevata all'interno del metodo {methodName}: {reason}", methodName, ex.Error.Reason);
			return;
		}
		catch (KafkaException ex) when (ot == OperationClient.Produce && methodName == "ProduceAsync")
		{
			_logger.LogError(ex, "Produzione del messaggio non eseguita correttamente");
		}
		catch (KafkaException ex) 
		{
			_logger.LogError(ex, "A Kafka error occurred while processing the task.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occurred while processing the task.");
		}
		 

	}
	public async Task<(bool shouldContinue, T?)> InvokeAsync<T>(Func<Task<T>> task, OperationClient ot = OperationClient.Unkwown, string methodName = "Unkwnown")
	{
		try
		{
			var result = await task.Invoke();
			return (false, result);
		}
		catch (ConsumeException ex) when (ot == OperationClient.Admin)
		{
			_logger.LogError(ex, "CreateTopicsException sollevata all'interno del metodo {methodName}", methodName);
			
		}

		catch (DeleteTopicsException ex) when (ot == OperationClient.Admin)
		{
			_logger.LogError(ex, "DeleteTopicsException sollevata all'interno del metodo {methodName}: {reason}", methodName, ex.Error.Reason);

		}
		catch (KafkaException ex) when (ot == OperationClient.CircuitBreaker && methodName == "ExecuteAsync") 
		{
			_logger.LogError(ex, "Errore generato dalla funzione {methodName} in CircuitBreaker", methodName);
			return (true, default); 
		}
		catch (KafkaException ex)
		{
			_logger.LogError(ex, "A Kafka error occurred while processing the task.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occurred while processing the task.");
		}
		return default;

	}

	public void Invoke(Action action, OperationClient ot = OperationClient.Unkwown, string methodName = "Unknown")
	{
		try
		{
			action.Invoke();
		}

		catch (KafkaException ex)
		{
			_logger.LogCritical(ex, "KafkaException sollevata all'interno del metodo {methodName}", methodName);
			return;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Eccezione sollevata all'interno del metodo {methodName}", methodName);
			return;
		}

	}





}
