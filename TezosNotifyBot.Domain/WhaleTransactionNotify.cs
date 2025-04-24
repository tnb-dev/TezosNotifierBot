using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Domain
{
	#nullable disable
	public class WhaleTransactionNotify
	{
		public int Id { get; set; }
		public WhaleTransaction WhaleTransaction { get; set; }
		public int WhaleTransactionId { get; set; }
		public User User { get; set; }
		public long UserId { get; set; }
	}
}
