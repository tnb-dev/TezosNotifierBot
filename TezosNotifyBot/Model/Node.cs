using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Model
{
	public class Node
	{
		public string Name { get; set; }
		public string Url { get; set; }

		internal string CheckStatus()
		{
			var client = new Tezos.Client(Url, null);
			try
			{
				var bh = client.GetBlockHeader("head");
				return $"{bh.level} ({bh.timestamp}) 🆗";
			}
			catch(Exception e)
			{
				return e.Message;
			}
		}
	}
}
