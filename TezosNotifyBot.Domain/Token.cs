using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Domain
{
	public class Token
	{
		public int Id { get; set; }
		public string ContractAddress { get; set; }
		public string Symbol { get; set; }
		public string Name { get; set; }
		public int Decimals { get; set; }
		public int Token_id { get; set; }
		public int Level { get; set; }
	}
}
