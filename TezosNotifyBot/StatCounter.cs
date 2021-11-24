using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot
{
	public class StatCounter
	{
		Dictionary<string, Dictionary<DateTime, int>> countStat = new Dictionary<string, Dictionary<DateTime, int>>();
		public void Count(string statName)
		{
			lock (countStat)
			{
				if (!countStat.ContainsKey(statName))
					countStat.Add(statName, new Dictionary<DateTime, int>());
				var stat = countStat[statName];
				var now = DateTime.Today.AddHours(DateTime.Now.Hour).AddMinutes(DateTime.Now.Minute);
				if (!stat.ContainsKey(now))
					stat.Add(now, 0);
				stat[now]++;
			}
		}

		Dictionary<string, Dictionary<DateTime, List<TimeSpan>>> timeSpanStat = new Dictionary<string, Dictionary<DateTime, List<TimeSpan>>>();
		public void AddTimeSpan(string statName, TimeSpan timeSpan)
		{
			lock(timeSpanStat)
			{
				if (!timeSpanStat.ContainsKey(statName))
					timeSpanStat.Add(statName, new Dictionary<DateTime, List<TimeSpan>>());
				var stat = timeSpanStat[statName];
				var now = DateTime.Today.AddHours(DateTime.Now.Hour).AddMinutes(DateTime.Now.Minute);
				if (!stat.ContainsKey(now))
					stat.Add(now, new List<TimeSpan>());
				stat[now].Add(timeSpan);
			}
		}

		public override string ToString()
		{
			string result = "";
			foreach (var stat in countStat)
			{
				result += $"{stat.Key}\n";
				result += $"time, count\n";
				foreach (var dt in stat.Value)
					result += $"{dt.Key.ToString("dd.MM.yyyy HH:mm")}, {dt.Value}\n";
			}
			result += $"\n";
			foreach (var stat in timeSpanStat)
			{
				result += $"{stat.Key}\n";
				result += $"time, count, avg\n";
				foreach (var dt in stat.Value)
					result += $"{dt.Key.ToString("dd.MM.yyyy HH:mm")}, {dt.Value.Count}, {dt.Value.Select(o => o.TotalSeconds).Average()}\n";
			}
			return result;
		}
	}
}
