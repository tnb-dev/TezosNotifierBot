using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TezosNotifyBot.Model
{
	public class Explorer
	{
		public string block(int blocknumber) => string.Format(blockurl, blocknumber);
		public string account(string addr) => string.Format(accounturl, addr);
		public string op(string ophash) => string.Format(opurl, ophash);
		public string url_vote(int period) => string.Format("https://www.tezosagora.org/period/{0}?utm_source=tezosnotifierbot", period);
		
		public override string ToString() => name;

		public int id { get; set; }
		public string name { get; set; }
		public string buttonprefix { get; set; }
		public string blockurl { get; set; }
		public string accounturl { get; set; }
		public string opurl { get; set; }

		static Dictionary<int, Explorer> explorers;
		static Explorer()
		{
			explorers = JsonConvert.DeserializeObject<Explorer[]>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "explorer.json"))).ToDictionary(o => o.id, o => o);
		}

		public static Explorer FromId(int id)
		{
			if (!explorers.ContainsKey(id))
				return explorers.First().Value;
			return explorers[id];
		}

		public static Explorer FromStart(string message)
		{
			return explorers.Values.FirstOrDefault(o => message.ToLower().Contains(o.buttonprefix)) ?? explorers[0];
		}

		public static IEnumerable<Explorer> All => explorers.Values;
	}
}