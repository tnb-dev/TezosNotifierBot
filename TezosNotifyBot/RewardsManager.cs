using System;
using System.Collections.Generic;
using System.Text;

/*
namespace TezosNotifyBot
{
	public class RewardsManager
	{
		Model.Repository repo;
		int lastLevel;
		public enum RewardType
		{
			StolenBaking = 0,
			Baking = 1,
			Endorsing = 2,
			MissedBaking = 3,
			MissedEndorsing = 4
		}
		public RewardsManager(Model.Repository repo)
		{
			this.repo = repo;
		}

		public void SetLastBlock(Level level)
		{
			actualRewards = repo.GetRewards(level.CycleFirst, level, false);
			maxRewards = repo.GetRewards(level.CycleFirst, level, true);
			prevActualRewards = repo.GetRewards(level.CycleFirst - Level.CycleSize, level.CycleLast - Level.CycleSize, false);
			prevMaxRewards = repo.GetRewards(level.CycleFirst - Level.CycleSize, level.CycleLast - Level.CycleSize, true);
			lastLevel = level.Height;
		}

		Dictionary<string, long> prevActualRewards = new Dictionary<string, long>();
		Dictionary<string, long> prevMaxRewards = new Dictionary<string, long>();

		Dictionary<string, long> actualRewards = new Dictionary<string, long>();
		Dictionary<string, long> maxRewards = new Dictionary<string, long>();

		public void BalanceUpdate(string @delegate, RewardType type, Level level, long amount, int slots = 0)
		{
			if ((int)level < lastLevel || (int)level > lastLevel + 1)
				SetLastBlock(level - 1);
			if (level.CyclePosition == 0 && lastLevel + 1 == level)
			{
				prevActualRewards = actualRewards;
				actualRewards = new Dictionary<string, long>();
				prevMaxRewards = maxRewards;
				maxRewards = new Dictionary<string, long>();
			}
			if (!actualRewards.ContainsKey(@delegate))
				actualRewards[@delegate] = 0;
			if (!maxRewards.ContainsKey(@delegate))
				maxRewards[@delegate] = 0;

			if ((int)type > 0)
				maxRewards[@delegate] = maxRewards[@delegate] + amount;
			if ((int)type <= 2)
				actualRewards[@delegate] = actualRewards[@delegate] + amount;
			repo.AddBalanceUpdate(@delegate, (int)type, level.Height + 1, amount, slots);
			lastLevel = level;
		}

		public long GetLastActualRewards(string address)
		{
			return prevActualRewards.ContainsKey(address) ? prevActualRewards[address] : 0;
		}

		public long GetLastMaxRewards(string address)
		{
			return prevMaxRewards.ContainsKey(address) ? prevMaxRewards[address] : 0;
		}
	}
}
*/