using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Kafka.MessageHandlers;

public class OperationMessage<TDto> where TDto : class
{
	public required string Operation { get; set; }
	public required TDto Dto { get; set; }


}
