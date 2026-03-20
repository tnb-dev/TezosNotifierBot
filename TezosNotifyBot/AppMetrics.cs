using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using TezosNotifyBot.Model;

namespace TezosNotifyBot
{
	public class AppMetrics
	{
		private readonly Counter<long> _messagesSent;
		private readonly Counter<long> _messagesReceived;
		private readonly Counter<long> _blocksProcessed;

		public AppMetrics(IMeterFactory meterFactory)
		{
			var meter = meterFactory.Create("TNB.Metrics");

			_messagesSent = meter.CreateCounter<long>("telegram.bot.messages.sent",
				description: "Number of messages sent to users");

			_messagesReceived = meter.CreateCounter<long>("telegram.bot.messages.received",
				description: "Number of messages received from users");

			_blocksProcessed = meter.CreateCounter<long>("blocks.processed",
				description: "Blocks processed");

			meter.CreateObservableGauge<long>("Notification queue 0", () => channels != null ? channels[0].Reader.Count : 0);
			meter.CreateObservableGauge<long>("Notification queue 1", () => channels != null ? channels[1].Reader.Count : 0);
			meter.CreateObservableGauge<long>("Notification queue 2", () => channels != null ? channels[2].Reader.Count : 0);
			meter.CreateObservableGauge<long>("Notification queue 3", () => channels != null ? channels[3].Reader.Count : 0);
			meter.CreateObservableGauge<long>("Notification queue 4", () => channels != null ? channels[4].Reader.Count : 0);
		}

		Channel<Notification>[] channels;
		public void SetChannels(Channel<Notification>[] channels)
		{
			this.channels = channels;
		}

		public void MessageSent(bool isSuccess = true)
		{
			_messagesSent.Add(1,
				new KeyValuePair<string, object?>("success", isSuccess));
		}

		public void MessageReceived()
		{
			_messagesReceived.Add(1);
		}

		public void BlockProcessed()
		{
			_blocksProcessed.Add(1);
		}
	}
}
