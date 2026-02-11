using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TezosNotifyBot.Domain;

namespace TezosNotifyBot.NotifyStats
{
	public class NotifyStatData
	{
		public int Last { get; set; }
		public int[] Count { get; set; } = new int[30];

		public int Total => Count.Sum(o => o);

		public void Inc()
		{
			int index = (int)DateTime.Today.Subtract(new DateTime(2026, 1, 1)).TotalDays % 30;
			Count[index]++;
		}

		const int MaxCount = 10000;

		public void Store(User user)
		{
			user.NotifyStat = JsonSerializer.Serialize(this);
		}

		private NotifyStatData()
		{			
		}

		public static NotifyStatData Load(User user) => JsonSerializer.Deserialize<NotifyStatData>(user.NotifyStat);
	}
}
