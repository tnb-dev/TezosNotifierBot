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
		private readonly Histogram<long>[] nq = new Histogram<long>[5];

		public AppMetrics(IMeterFactory meterFactory)
		{
			var meter = meterFactory.Create("TNB.Metrics");

			_messagesSent = meter.CreateCounter<long>("telegram.bot.messages.sent",
				description: "Number of messages sent to users");

			_messagesReceived = meter.CreateCounter<long>("telegram.bot.messages.received",
				description: "Number of messages received from users");

			_blocksProcessed = meter.CreateCounter<long>("blocks.processed",
				description: "Blocks processed");

			nq[0] = meter.CreateHistogram<long>("notification.queue.0");
			nq[1] = meter.CreateHistogram<long>("notification.queue.1");
			nq[2] = meter.CreateHistogram<long>("notification.queue.2");
			nq[3] = meter.CreateHistogram<long>("notification.queue.3");
			nq[4] = meter.CreateHistogram<long>("notification.queue.4");
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

		public void RecordNQ(int size, int priority) => nq[priority].Record(size);
	}
}
