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
		private readonly Gauge<long> _blockProcessingLag;
		private readonly Gauge<long> _blockProcessingTime;
		private readonly Gauge<long> _messagesSentPerBlock;
		private readonly Gauge<long> _blockTxCount;
		private readonly Gauge<long> _blockApiRequestCount;
		long _messagesPerBlock = 0;
		long _requestsPerBlock = 0;
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

			_blockProcessingLag = meter.CreateGauge<long>("block.processing.lag", "blocks", "Block processing lag");

			_blockProcessingTime = meter.CreateGauge<long>("block.processing.time", "ms", "Block processing time");

			_messagesSentPerBlock = meter.CreateGauge<long>("messages.sent.per.block", description: "Messages sent per block");

			_blockTxCount = meter.CreateGauge<long>("block.transactions.count", "count", "Block transactions count");

			_blockApiRequestCount = meter.CreateGauge<long>("block.api.requests", description: "API requests per block");

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
			_messagesPerBlock++;
		}

		public void MessageReceived() => _messagesReceived.Add(1);

		public void BlockProcessed()
		{
			_blocksProcessed.Add(1);
			_messagesSentPerBlock.Record(_messagesPerBlock);
			_messagesPerBlock = 0;
			_blockApiRequestCount.Record(_requestsPerBlock);
			_requestsPerBlock = 0;
		}
		
		public void BlockProcessingLag(int lag) => _blockProcessingLag.Record(lag);

		public void BlockProcessingTime(long time) => _blockProcessingTime.Record(time);

		public void StartProcessing() => _messagesPerBlock = 0;

		public void BlockTxCount(int txCount) => _blockTxCount.Record(txCount);

		public void ApiRequestInc() => _requestsPerBlock++;

		public void RecordNQ(int size, int priority) => nq[priority].Record(size);
	}
}
