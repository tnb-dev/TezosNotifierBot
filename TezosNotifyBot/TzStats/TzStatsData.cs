using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.TzStats
{
	public class TzStatsData
	{
		Worker worker;
		public TzStatsData(Worker worker)
		{
			this.worker = worker;
		}

		Dictionary<int, Cycle> cycles = new Dictionary<int, Cycle>();

		public Cycle GetCycle(Level level)
		{
			int cycle = level.Cycle;
			if (!cycles.ContainsKey(cycle))
				LoadCycle(level);
			return cycles[cycle];
		}

		public void LoadCycle(Level level)
		{
			int cycle = level.Cycle;
			lock(cycles)
				if (!cycles.ContainsKey(cycle))
				{
					using (var wc = new WebClientBase(null))
					{
						string str = wc.Download($"https://api.tzstats.com/explorer/cycle/{cycle}");
						var c = JsonConvert.DeserializeObject<TzStats.Cycle>(str);
						cycles[cycle] = c;
					}					
				}
		}
	}
}
