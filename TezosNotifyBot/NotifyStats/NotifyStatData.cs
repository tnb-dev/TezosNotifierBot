using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
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

		int Current = (int)DateTime.Today.Subtract(new DateTime(2026, 1, 1)).TotalDays;
		int Index => Current % 30;

		public void Inc()
		{
			Count[Index]++;
		}

		public const int MaxCount = 10;

		public void Store(User user)
		{
			user.NotifyStat = JsonSerializer.Serialize(this);
		}

		public static NotifyStatData Load(User user) => Load(user.NotifyStat);

		public static NotifyStatData Load(string notifyStat)
		{
			var nsd = notifyStat == null ? new NotifyStatData() : JsonSerializer.Deserialize<NotifyStatData>(notifyStat);
			while (nsd.Current > nsd.Last)
				nsd.Count[++nsd.Last % 30] = 0;

			return nsd;
		}
	}
}
