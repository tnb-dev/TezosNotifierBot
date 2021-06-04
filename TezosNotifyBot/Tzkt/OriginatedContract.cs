using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class OriginatedContract
	{
		public string kind { get; set; }
		public string address { get; set; }
		public int typeHash { get; set; }
		public int codeHash { get; set; }
	}
}
