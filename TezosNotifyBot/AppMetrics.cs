using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TezosNotifyBot
{
	public static class AppMetrics
	{
		private static readonly Meter Meter = new("TNB.Metrics", "1.0");

		public static readonly Counter<long> MessagesSent =
			Meter.CreateCounter<long>("messages.sent", "messages", "Total messages sent to users");

		public static readonly Counter<long> MessagesReceived =
			Meter.CreateCounter<long>("messages.received", "messages", "Total messages received from users");

		public static readonly Counter<long> BlocksProcessed =
			Meter.CreateCounter<long>("blocks.processed", "blocks", "Total blocks processed");
	}
}
