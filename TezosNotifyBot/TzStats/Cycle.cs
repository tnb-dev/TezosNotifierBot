using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.TzStats
{
	public class Cycle
	{
		public int cycle { get; set; }
		public int start_height { get; set; }
		public int end_height { get; set; }
		public DateTime start_time { get; set; }
		public DateTime end_time { get; set; }
		public int rolls { get; set; }
		public int roll_owners { get; set; }
		public SnapshotCycle snapshot_cycle { get; set; }
		public decimal staking_supply { get; set; }
	}
	public class SnapshotCycle
	{
		public int cycle { get; set; }
		public int rolls { get; set; }
		public int roll_owners { get; set; }
	}
}
