using System;
using System.Collections.Generic;

namespace TezosNotifyBot.Domain
{
	public class WhaleTransaction
	{
		public int Id { get; set; }
		public string FromAddress { get; set; }
		public string ToAddress { get; set; }
		public string OpHash { get; set; }
		public decimal Amount { get; set; }
		public int Level { get; set; }
		public DateTime Timestamp { get; set; }
		public IList<WhaleTransactionNotify> Notifications { get; } = new List<WhaleTransactionNotify>();
	}
}
