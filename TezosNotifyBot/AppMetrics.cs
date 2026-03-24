using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TezosNotifyBot
{
	public class AppMetrics
	{
		private readonly Counter<long> _messagesSent;
		private readonly Counter<long> _messagesReceived;
		private readonly Counter<long> _blocksProcessed;
		private readonly Gauge<long> _blockProcessingLag;
		private readonly Gauge<long> _blockProcessingTime;

		public AppMetrics(IMeterFactory meterFactory)
		{
			var meter = meterFactory.Create("TNB.Metrics");

			_messagesSent = meter.CreateCounter<long>("telegram.bot.messages.sent",
				description: "Number of messages sent to users");

			_messagesReceived = meter.CreateCounter<long>("telegram.bot.messages.received",
				description: "Number of messages received from users");

			_blocksProcessed = meter.CreateCounter<long>("blocks.processed",
				description: "Blocks processed");

			_blockProcessingLag = meter.CreateGauge<long>("Block processing lag");

			_blockProcessingTime = meter.CreateGauge<long>("Block processing time", "ms");
		}

		public void MessageSent(bool isSuccess = true)
		{
			_messagesSent.Add(1,
				new KeyValuePair<string, object?>("success", isSuccess));
		}

		public void MessageReceived() => _messagesReceived.Add(1);
		
		public void BlockProcessed() => _blocksProcessed.Add(1);
		
		public void BlockProcessingLag(int lag) => _blockProcessingLag.Record(lag);

		public void BlockProcessingTime(long time) => _blockProcessingTime.Record(time);
	}
}
