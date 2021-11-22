using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class Delegator
	{
		public string type { get; set; }
		public string address { get; set; }
		public long balance { get; set; }
		public int delegationLevel { get; set; }
		public DateTime delegationTime { get; set; }
	}
}
