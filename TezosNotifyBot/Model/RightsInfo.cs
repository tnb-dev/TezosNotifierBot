using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Model
{
	public class RightsInfo
	{
		public int BakingCount { get; set; }
		public int EndorsingCount { get; set; }
		public int EndorsingSlotsCount { get; set; }
		public int FirstBakingLevel { get; set; }
		public DateTime FirstBakingTime { get; set; }
		public int FirstEndorsingLevel { get; set; }
		public DateTime FirstEndorsingTime { get; set; }
		public string Status { get; set; }
		public int Count => BakingCount + EndorsingCount;
		public RightsInfo(IEnumerable<TezosNotifyBot.Tzkt.Right> rights)
		{
			foreach (var r in rights)
			{
				if (r.type == "baking")
					BakingCount++;
				if (BakingCount == 1)
				{
					FirstBakingLevel = r.level;
					FirstBakingTime = r.timestamp;
				}
				if (r.type == "endorsing")
				{
					EndorsingCount++;
					EndorsingSlotsCount += r.slots.Value;
				}
				if (EndorsingCount == 1)
				{
					FirstEndorsingLevel = r.level;
					FirstEndorsingTime = r.timestamp;
				}
			}
		}
		public RightsInfo(TezosNotifyBot.Tzkt.Right r)
		{
			if (r.type == "baking")
			{ 
				BakingCount++;
				FirstBakingLevel = r.level;
				FirstBakingTime = r.timestamp;
			}
			if (r.type == "endorsing")
			{
				EndorsingCount++;
				EndorsingSlotsCount += r.slots.Value;
				FirstEndorsingLevel = r.level;
				FirstEndorsingTime = r.timestamp;
			}
			Status = r.status;
		}
	}
}
