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
		private readonly Gauge<long> _messagesSentPerBlock;
		long _messagesPerBlock = 0;

		public AppMetrics(IMeterFactory meterFactory)
		{
			var meter = meterFactory.Create("TNB.Metrics");

			_messagesSent = meter.CreateCounter<long>("telegram.bot.messages.sent",
				description: "Number of messages sent to users");

			_messagesReceived = meter.CreateCounter<long>("telegram.bot.messages.received",
				description: "Number of messages received from users");

			_blocksProcessed = meter.CreateCounter<long>("blocks.processed",
				description: "Blocks processed");

			_blockProcessingLag = meter.CreateGauge<long>("block.processing.lag", "blocks", "Block processing lag");

			_blockProcessingTime = meter.CreateGauge<long>("block.processing.time", "ms", "Block processing time");

			_messagesSentPerBlock = meter.CreateGauge<long>("messages.sent.per.block", description: "Messages sent per block");
		}

		public void MessageSent(bool isSuccess = true)
		{
			_messagesSent.Add(1,
				new KeyValuePair<string, object?>("success", isSuccess));
		}

		public void MessageReceived() => _messagesReceived.Add(1);

		public void BlockProcessed()
		{
			_blocksProcessed.Add(1);
			_messagesSentPerBlock.Record(_messagesPerBlock);
			_messagesPerBlock = 0;
		}
		
		public void BlockProcessingLag(int lag) => _blockProcessingLag.Record(lag);

		public void BlockProcessingTime(long time) => _blockProcessingTime.Record(time);

		public void StartProcessing() => _messagesPerBlock = 0;
	}
}
