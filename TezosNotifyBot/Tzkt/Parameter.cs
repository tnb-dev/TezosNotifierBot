using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class Parameter
	{
		public string entrypoint { get; set; }
		public object value { get; set; }
		//public Value value { get; set; }
	}

	public class Value
	{
		public string to { get; set; }
		public string from { get; set; }
		public string value { get; set; }
	}
}
