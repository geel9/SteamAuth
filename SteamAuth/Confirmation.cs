using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SteamAuth
{
	public class Confirmation
	{
		[JsonProperty(PropertyName = "id")]
		public ulong ID { get; set; }

		[JsonProperty(PropertyName = "nonce")]
		public ulong Key { get; set; }

		[JsonProperty(PropertyName = "creator_id")]
		public ulong Creator { get; set; }

		[JsonProperty(PropertyName = "type")]
		[JsonConverter(typeof(StringEnumConverter))]
		public ConfirmationType ConfType { get; set; } = ConfirmationType.Unknown;
		
		public enum ConfirmationType
		{
			GenericConfirmation,
			Trade,
			MarketSellTransaction,
			Unknown
		}
	}
	
	public class ConfirmationsResponse 
	{
		[JsonProperty("success")]
		public bool Success { get; set; }
		
		[JsonProperty("needauth")]
		public bool NeedAuthentication { get; set; }

		[JsonProperty("conf")]
		public Confirmation[] Confirmations { get; set; }
	}
}
