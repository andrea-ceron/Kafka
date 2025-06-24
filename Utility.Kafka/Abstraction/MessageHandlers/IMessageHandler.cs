using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Kafka.Abstraction.MessageHandlers;

public interface IMessageHandler<TKey,TValue>
	where TKey : class
	where TValue : class	
{
	Task OnMessageReceivedAsync(TKey key, TValue message, CancellationToken cancellationToken = default);

}
