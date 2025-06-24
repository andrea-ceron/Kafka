using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Kafka.MessageHandlers;

public class Operations
{
	public enum QueryType
	{
		Insert, 
		Update, 
		Delete
	}
	public const string Insert = "I";

	public const string Update = "U";

	public const string Delete = "D";

	public static string GetStringValue(Enum valueEnum)
	{
		return valueEnum switch
		{
			QueryType.Insert => Insert,
			QueryType.Update => Update,
			QueryType.Delete => Delete,
			_ => throw new ArgumentOutOfRangeException(nameof(valueEnum), $"{nameof(valueEnum)} contains an invalid value '{valueEnum}'")
		};
	}

};