using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utility.Kafka.Abstraction.MessageHandlers;
using Utility.Kafka.ExceptionManager;

namespace Utility.Kafka.MessageHandlers
{
	public abstract class OperationMessageHandlerBase<TMessageDto> (ErrorManagerMiddleware errorManager)
		: IMessageHandler<string, string>
		 where TMessageDto : class
	{
		private ErrorManagerMiddleware _errorManager = errorManager;
		public async Task OnMessageReceivedAsync(string key, string message, CancellationToken cancellationToken = default)
		{
			await Task.Run(async () =>
			{
				OperationMessage<TMessageDto>? operationMessage = null;
				await _errorManager.InvokeAsync(async () =>
				{
					operationMessage = JsonSerializer.Deserialize<OperationMessage<TMessageDto>>(message);
					if (operationMessage == null)
						throw new ArgumentNullException(nameof(operationMessage), "Deserialized operation message is null.");
					switch (operationMessage.Operation)
					{
						case Operations.Insert:
							await InsertAsync(operationMessage.Dto, cancellationToken);
							break;
						case Operations.Update:
							await UpdateAsync(operationMessage.Dto, cancellationToken);
							break;
						case Operations.Delete:
							await DeleteAsync(operationMessage.Dto, cancellationToken);
							break;
						default:
							throw new InvalidOperationException( "Non è possibile chiamare una funzione al di fuori di quelle definite");
							break;
					}
				}, ErrorManagerMiddleware.OperationClient.OperationMessageHandlerBase, "OnMessageReceivedAsync");
			

				
		}, cancellationToken);
		}
		protected abstract Task InsertAsync(TMessageDto messageDto, CancellationToken cancellationToken = default);
		protected abstract Task UpdateAsync(TMessageDto messageDto, CancellationToken cancellationToken = default);
		protected abstract Task DeleteAsync(TMessageDto messageDto, CancellationToken cancellationToken = default);



	}
}
