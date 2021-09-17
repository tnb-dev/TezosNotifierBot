using System;
using TezosNotifyBot.Shared.Extensions;

namespace TezosNotifyBot.Tzkt
{
	public class Address
	{
		public string? alias { get; set; }
		public string address { get; set; }
		
		public string ShortAddr() => address.ShortAddr();

		public string DisplayName() => string.IsNullOrEmpty(alias) ? ShortAddr() : alias;
	}
}
