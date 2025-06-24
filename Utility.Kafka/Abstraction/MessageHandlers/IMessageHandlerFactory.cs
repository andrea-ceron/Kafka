using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Kafka.Abstraction.MessageHandlers;
public interface IMessageHandlerFactory<TKey, TValue>
	where TKey : class
	where TValue : class
{
	IMessageHandler<TKey, TValue> Create(string topic, IServiceProvider serviceProvider);
}
