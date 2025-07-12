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
		Delete,
		CompensationInsert,
		CompensationUpdate,
		CompensationDelete,
	}
	public const string Insert = "I";

	public const string Update = "U";

	public const string Delete = "D";
	public const string CompensationInsert = "CI";
	public const string CompensationUpdate = "CU";
	public const string CompensationDelete = "CD";

	public static string GetStringValue(Enum valueEnum)
	{
		return valueEnum switch
		{
			QueryType.Insert => Insert,
			QueryType.Update => Update,
			QueryType.Delete => Delete,
			QueryType.CompensationInsert => CompensationInsert,
			QueryType.CompensationUpdate => CompensationUpdate,
			QueryType.CompensationDelete => CompensationDelete,

			_ => throw new ArgumentOutOfRangeException(nameof(valueEnum), $"{nameof(valueEnum)} contains an invalid value '{valueEnum}'")
		};
	}

};