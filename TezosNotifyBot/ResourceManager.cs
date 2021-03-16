using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Model;

namespace TezosNotifyBot
{
	public class ResourceManager
	{
		Dictionary<(string, string), string> resList = new Dictionary<(string, string), string>();
		CodingSeb.ExpressionEvaluator.ExpressionEvaluator ee = new CodingSeb.ExpressionEvaluator.ExpressionEvaluator();
		public ResourceManager()
		{
			ee.StaticTypesForExtensionsMethods.Add(typeof(Utils));
		}
		public void LoadResources(string resFileName)
		{
			var lines = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resFileName), Encoding.UTF8);
			string currentItem = null;
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].StartsWith('%'))
					continue;
				if (lines[i].StartsWith('#'))
				{
					if (currentItem != null)
						Add(currentItem);
					currentItem = lines[i].Substring(1);
					continue;
				}
				if (currentItem != null)
					currentItem += '\n';
				currentItem += lines[i];
			}
			if (currentItem != null)
			{
				Add(currentItem);
			}
			foreach (var name in Enum.GetNames(typeof(Res)))
			{
				if (!resList.ContainsKey((name, "en")))
					throw new ApplicationException($"Resource {name};en doesn't exists");
				if (!name.StartsWith("Twitter") && !resList.ContainsKey((name, "ru")))
					throw new ApplicationException($"Resource {name};ru doesn't exists");
			}
		}
		void Add(string res)
		{
			var s1 = res.IndexOf(';');
			if (s1 < 0)
				return;
			string name = res.Substring(0, s1);
			var s2 = res.IndexOf(';', s1 + 1);
			if (s2 < 0)
				return;
			string lang = res.Substring(s1 + 1, s2 - s1 - 1);
			if (resList.ContainsKey((name, lang)))
				throw new ApplicationException($"Resource {name};{lang} already exists");
			if (lang != "ru" && lang != "en")
				throw new ApplicationException($"Resource {name};{lang} - invalid language");
			resList[(name, lang)] = res.Substring(s2 + 1);
		}

		public string Get(Res name, ContextObject data)
		{
			return Get(name, data.u?.Language ?? "en", data);
		}

		public string Get<TContext>(Res key, string lang, TContext data)
			where TContext: class
		{
			if (lang != "ru")
				lang = "en";
			if (!resList.ContainsKey((key.ToString(), lang)))
				throw new ApplicationException($"Text Resource {key};{lang} not found");
			var resStr = resList[(key.ToString(), lang)];
			if (!resStr.Contains("{"))
				return resStr;
			resStr = "\"" + resStr.Replace("{", "\"+").Replace("}", "+\"") + "\"";
			lock (ee)
			{
				try
				{
					ee.Context = data;
					return Convert.ToString(ee.Evaluate(resStr));
				}
				finally
				{
					ee.Context = null;
				}
			}
		}
	}

	public class ContextObject
	{
		public User u { get; set; }
		public UserAddress ua { get; set; }
		public Explorer t => (u != null ? Explorer.FromId(u.Explorer) : Explorer.FromId(0));
		public Tezos.MarketData md { get; set; } = new Tezos.MarketData();
		public Proposal p { get; set; }

		public TezosRelease r { get; set; }
		
		public string OpHash { get; set; }
		public int TotalRolls { get; set; }
		public int Block { get; set; }
		public int Period { get; set; }
		public int Priority { get; set; }
		public decimal Amount { get; set; }
		public Token Token { get; set; }
		public UserAddress ua_from { get; set; }
		public UserAddress ua_to { get; set; }
		public int Minutes { get; set; }
		public int Cycle { get; set; }
		
		public UserAddress Delegate { get; set; }

		public static implicit operator ContextObject(User user) => new ContextObject { u = user };
		public static implicit operator ContextObject(UserAddress userAddress) => new ContextObject { u = userAddress.User, ua = userAddress };
		public static implicit operator ContextObject((UserAddress userAddress, Tezos.MarketData md) uamd) => new ContextObject { u = uamd.userAddress.User, ua = uamd.userAddress, md = uamd.md };
		public static implicit operator ContextObject((UserAddress userAddress, Proposal p) uap) => new ContextObject { u = uap.userAddress.User, ua = uap.userAddress, p = uap.p };
		public static implicit operator ContextObject((User user, TezosRelease release) data) 
			=> new ContextObject { u = data.user, r = data.release };
	}

	public enum Res
	{
		NetworkIssue,
		TestingVoteSuccess,
		DelegateDidNotVoted,
		TestingVoteFailed,
		PromotionVoteSuccess,
		PromotionVoteFailed,
		DoubleBakingOccured,
		DoubleBakingEvidence,
		NewProposal,
		SupplyProposal,
		QuorumReached,
		NewDelegation,
		UnDelegation,
		WhaleTransaction,
		OutgoingTransaction,
		OutgoingTransactions,
		To,
		NotAllShown,
		ActualBalance,
		CurrentBalance,
		IncomingTransaction,
		IncomingTransactions,
		From,
		MissedBaking,
		StoleBaking,
		MissedEndorsing,
		SkippedEndorsing,
		RewardDeliveredItem,
		RewardDelivered,
		CycleCompleted,
		Performance,
		Accrued,
		ProposalSelectedForVotingOne,
		ProposalSelectedForVoting,
		ProposalSelectedItem,
		ProposalSelectedMany,
		AddressDeleted,
		AddressNotExist,
		EnterAmountThreshold,
		EnterNewName,
		EnterDlgAmountThreshold,
		ChooseExplorer,
		WhaleAlertsTip,
		WhaleAlertSet,
		NetworkIssueAlertsTip,
		NetworkIssueAlertSet,
		Welcome,
		MessageDelivered,
		WelcomeBack,
		SupportReply,
		SeeYou,
		WriteHere,
		MessageSentToSupport,
		ThresholdEstablished,
		UnrecognizedCommand,
		DlgThresholdEstablished,
		EnterDelegatorsBalanceThreshold,
		ChangedDelegatorsBalanceThreshold,
		AddressRenamed,
		NewAddressHint,
		AddressAdded,
		StakingInfo,
		Delegate,
		IncorrectTezosAddress,
		FreeSpace,
		AveragePerformance,
		NotifyIn,
		Events,
		TransactionNotifications,
		AmountThreshold,
		DelegationNotifications,
		DelegationAmountThreshold,
		RewardNotifications,
		CycleCompletionNotifications,
		MissesNotifications,
		NoAddresses,
		Search,
		Explorer,
		HashTags,
		WhaleAlerts,
		NetworkIssueAlerts,
		VotingNotify,
		PayoutNotifyStatus,
		PayoutNotifyToggle,
		ReleaseNotify,
		DelegatorsBalanceThreshold,
		DelegatorsBalanceNotifyStatus,
		DelegatorsBalanceNotifyToggle,
		DelegatorsBalanceThresholdButton,
		Off,
		Delete,
		ManageAddress,
		TransactionNotify,
		SetThreshold,
		RenameAddress,
		AddAddress,
		DelegationNotify,
		SetDlgThreshold,
		RewardNotify,
		CycleNotify,
		MissesNotify,
		NewAddress,
		MyAddresses,
		Contact,
		Settings,
		GoBack,
		BallotProposal_yay,
		BallotProposal_nay,
		BallotProposal_pass,
		Watchers,
		NotifyFollowers,
		EnterMessageForAddressFollowers,
		MessageDeliveredForUsers,
		TwitterWhaleTransaction,
		TwitterNewProposal,
		TwitterNetworkAlert,
		Donate,
		DonateInfo,
		TwitterQuorumReached,
		TezosReleaseWithLink,
		TezosRelease,
		Tokens,
		TokenBalance,
		LastSeen,
		LastActive,
		RevelationPenalty,
		Payout,
		DelegatorsBalance,
		CurrentDelegatorBalance,
		AwardAvailable,
		AwardNotify,
		AwardAvailableNotifyStatus
	}
}
