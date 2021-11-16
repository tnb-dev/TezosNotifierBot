using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Tzkt;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Model;

namespace TezosNotifyBot
{
	partial class TezosBot
	{
        string votingStatus;
		void VotingNotify(Block block, Cycle cycle, ITzKtClient tzKtClient)
        {
            var periods = tzKtClient.GetVotingPeriods();
            var period = periods.Single(p => p.firstLevel <= block.Level && block.Level <= p.lastLevel);
            votingStatus = "\n\n" + period.kind[0].ToString().ToUpper() + period.kind.Substring(1) + $" ends {period.endTime.ToString("MMMMM d a\\t HH:mm")} UTC";

            // Proposals
            foreach (var proposal in block.Proposals)
            {
                var hash = proposal.Proposal.Hash;
                var from = proposal.@Delegate.Address;
                var p = repo.GetProposal(hash);
                if (p == null)
                {
                    p = repo.AddProposal(hash, from, proposal.Period.Index);
                    p.VotedRolls = proposal.Rolls;

                    foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
                    {
                        var ua = repo.GetUserAddresses(u.Id).FirstOrDefault(o => o.Address == from);
                        if (ua == null)
                            ua = new UserAddress { Address = from, Name = repo.GetDelegateName(from) };
                        ua.StakingBalance = proposal.Rolls * TezosConstants.TokensPerRoll;
                        var result = resMgr.Get(Res.NewProposal,
                            new ContextObject
                            {
                                ua = ua,
                                p = p,
                                u = u,
                                OpHash = proposal.Hash,
                                Block = block.Level,
                                Period = proposal.Period.Index
                            });
                        if (!u.HideHashTags)
                            result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
                        SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
                    }

                    //Twitter
                    {
                        var twText = resMgr.Get(Res.TwitterNewProposal,
                            new ContextObject
                            {
                                ua = new UserAddress
                                {
                                    Address = from,
                                    Name = repo.GetDelegateName(from),
                                    StakingBalance = proposal.Rolls * TezosConstants.TokensPerRoll
                                },
                                p = p,
                                OpHash = proposal.Hash,
                                Period = proposal.Period.Index
                            });
                        twitter.TweetAsync(twText);
                    }
                }
                else
				{
                    var prop = tzKtClient.GetProposals(proposal.Period.Epoch).Single(p => p.hash == hash);
                    p.VotedRolls = prop.rolls;
                    foreach (var ua in repo.GetUserAddresses(from))
                    {
                        if (ua.User.VotingNotify)
                        {
                            ua.StakingBalance = proposal.Rolls * TezosConstants.TokensPerRoll;
                            var result = resMgr.Get(Res.SupplyProposal,
                                new ContextObject
                                {
                                    ua = ua,
                                    p = p,
                                    u = ua.User,
                                    OpHash = proposal.Hash,
                                    TotalRolls = period.totalRolls.Value,
                                    Block = block.Level,
                                    Period = proposal.Period.Index
                                });
                            if (!ua.User.HideHashTags)
                                result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
                            SendTextMessage(ua.UserId, result, ReplyKeyboards.MainMenu(resMgr, ua.User));
                        }
                    }

                    if (Config.Telegram.VotingChat != 0)
					{
                        var ua = new UserAddress { Address = from, Name = repo.GetDelegateName(from) };
                        ua.StakingBalance = proposal.Rolls * TezosConstants.TokensPerRoll;
                        var result = resMgr.Get(Res.SupplyProposal,
                            new ContextObject
                            {
                                ua = ua,
                                p = p,
                                OpHash = proposal.Hash,
                                TotalRolls = period.totalRolls.Value,
                                Block = block.Level,
                                Period = proposal.Period.Index
                            });
                        SendTextMessage(Config.Telegram.VotingChat, result, null);
                    }
                }
            }

            // Ballots
            foreach(var ballot in block.Ballots)
            {
                var from = ballot.@Delegate.Address;
                var hash = ballot.Proposal.Hash;

                //var listings = _nodeManager.Client.GetVoteListings(header.hash);
                //int rolls = listings.Single(o => o.pkh == from).rolls;
                int allrolls = period.totalRolls.Value;

                var p = repo.GetProposal(hash);
                if (p == null)
                {
                    p = repo.AddProposal(hash, from, ballot.Period.Index);
                    NotifyDev($"⚠️ Proposal {p.Name} missed in database, added", 0);
                }                                

                Res bp = Res.BallotProposal_pass;
                if (ballot.Vote == "yay")
                    bp = Res.BallotProposal_yay;
                if (ballot.Vote == "nay")
                    bp = Res.BallotProposal_nay;
                foreach (var ua in repo.GetUserAddresses(from))
                {
                    if (ua.User.VotingNotify)
                    {
                        ua.StakingBalance = ballot.Rolls * TezosConstants.TokensPerRoll;
                        var result = resMgr.Get(bp,
                            new ContextObject
                            {
                                ua = ua,
                                p = p,
                                u = ua.User,
                                OpHash = ballot.Hash,
                                TotalRolls = allrolls,
                                Block = block.Level,
                                Period = ballot.Period.Index
                            });
                        if (!ua.User.HideHashTags)
                            result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
                        SendTextMessage(ua.UserId, result, ReplyKeyboards.MainMenu(resMgr, ua.User));
                    }
                }

                if (Config.Telegram.VotingChat != 0)
				{
                    var ua = new UserAddress { Address = from, Name = repo.GetDelegateName(from) };
                    ua.StakingBalance = ballot.Rolls * TezosConstants.TokensPerRoll;                    
                    var result = resMgr.Get(bp,
                        new ContextObject
                        {
                            ua = ua,
                            p = p,
                            OpHash = ballot.Hash,
                            TotalRolls = allrolls,
                            Block = block.Level,
                            Period = ballot.Period.Index
                        });
                    SendTextMessage(Config.Telegram.VotingChat, result, null);
                }
                // Check quorum

                var participation = period.yayRolls + period.nayRolls + period.passRolls;

                var quorum = period.ballotsQuorum;

                var curQuorum = participation.Value * 100M / allrolls;
                votingStatus += $"\nQuorum: {curQuorum.ToString("0.00")}%/{quorum.Value.ToString("0.00")}%";
                votingStatus += $"\nSupermajority: {curQuorum.ToString("0.00")}%/{period.supermajority}%";
                
                if (participation * 100M / allrolls >= quorum &&
                    (participation - ballot.Rolls) * 100M / allrolls < quorum)
                {
                    foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
                    {
                        var result = resMgr.Get(Res.QuorumReached,
                            new ContextObject { u = u, p = p, Block = block.Level, Period = ballot.Period.Index });
                        if (!u.HideHashTags)
                            result += "\n\n#proposal" + p.HashTag();
                        SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
                    }

                    {
                        var twText = resMgr.Get(Res.TwitterQuorumReached,
                            new ContextObject { p = p, Block = block.Level, Period = ballot.Period.Index });
                        twitter.TweetAsync(twText);
                    }
                }
            }
        
            if (block.Level == period.firstLevel)
			{
                var prevPeriod = periods.Single(p => p.index == period.index - 1);
                if (period.kind == "exploration")
				{
                    var proposals = tzKtClient.GetProposals(period.epoch);
                    var supporters = tzKtClient.GetUpvotes(period.epoch).GroupBy(o => o.Proposal.Hash)
                        .ToDictionary(o => o.Key, o => o.Select(p => p.Delegate.Address).ToList());

                    foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
                    {
                        var t = Explorer.FromId(u.Explorer);
                        if (proposals.Count == 1)
                        {
                            string propHash = proposals[0].hash;
                            var p = repo.GetProposal(propHash);
                            if (p == null)
                                p = repo.AddProposal(propHash, null, prevPeriod.index);

                            var delegateList = supporters[propHash];
                            // Список поддержавших делегатов, которые мониторит юзер
                            var addrList = repo.GetUserAddresses(u.Id).Where(o => delegateList.Contains(o.Address))
                                .ToList();
                            p.Delegates = addrList;
                            p.VotedRolls = proposals.Single().rolls;
                            string tags = "";
                            string result = resMgr.Get(Res.ProposalSelectedForVotingOne,
                                new ContextObject { p = p, u = u, Block = block.Level, Period = period.index });
                            if (addrList.Count() > 0)
                            {
                                tags = String.Join("", addrList.Select(o => o.HashTag()));
                                result += String.Join(", ",
                                    p.Delegates.Select(o =>
                                        "<a href='" + t.account(o.Address) + "'>" + o.DisplayName() + "</a>").ToArray());
                                result += "\n";
                            }

                            result += resMgr.Get(Res.ProposalSelectedForVoting,
                                new ContextObject { p = p, u = u, Block = block.Level, Period = period.index });

                            if (!u.HideHashTags)
                                result += "\n\n#proposal" + p.HashTag() + tags;
                            SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
                        }
                        else
                        {
                            string propItems = "";
                            string tags = "";
                            foreach (var prop in proposals)
                            {
                                string propHash = prop.hash;
                                var delegateList = supporters[propHash];
                                var addrList = repo.GetUserAddresses(u.Id).Where(o => delegateList.Contains(o.Address))
                                    .ToList();
                                string delegateListString = "";
                                if (addrList.Count() > 0)
                                {
                                    delegateListString = String.Join(", ",
                                        addrList.Select(o => $"<a href='{t.account(o.Address)}'>{o.DisplayName()}</a>")
                                            .ToArray());
                                    tags += String.Join("", addrList.Select(o => o.HashTag()));
                                }

                                var p = repo.GetProposal(propHash);
                                if (p == null)
                                    p = repo.AddProposal(propHash, null, prevPeriod.index);
                                p.VotedRolls = prop.rolls;
                                p.Delegates = addrList;
                                propItems +=
                                    resMgr.Get(Res.ProposalSelectedItem,
                                        new ContextObject { p = p, u = u, Block = block.Level, Period = period.index }) + "\n" +
                                    delegateListString;
                            }

                            {
                                var prop = proposals.OrderByDescending(o => o.rolls).First();
                                string propHash = prop.hash;
                                var p = repo.GetProposal(propHash);
                                p.VotedRolls = prop.rolls;
                                var delegateList = supporters[propHash];
                                // Список поддержавших делегатов, которые мониторит юзер
                                var addrList = repo.GetUserAddresses(u.Id).Where(o => delegateList.Contains(o.Address))
                                    .ToList();
                                p.Delegates = addrList;
                                string result =
                                    resMgr.Get(Res.ProposalSelectedMany,
                                        new ContextObject { p = p, u = u, Block = block.Level, Period = period.index }) + "\n" +
                                    propItems + "\n\n" +
                                    resMgr.Get(Res.ProposalSelectedForVoting,
                                        new ContextObject { p = p, u = u, Block = block.Level, Period = period.index });
                                if (!u.HideHashTags)
                                    result += "\n\n#proposal" + p.HashTag() + tags;
                                SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
                            }
                        }
                    }
                }
			
                if (prevPeriod.kind == "exploration" && period.kind == "testing")
				{
                    var proposal = tzKtClient.GetProposals(period.epoch).OrderByDescending(p => p.rolls).First();
                    var p = repo.GetProposal(proposal.hash);
                    foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
					{
						var result = resMgr.Get(Res.TestingVoteSuccess,
							new ContextObject { p = p, u = u, Block = block.Level, Period = period.index });
						if (!u.HideHashTags)
							result += "\n\n#proposal" + p.HashTag();
						SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
					}
                        
                    // Делегат не проголосовал
                    var ballots = tzKtClient.GetBallots(prevPeriod.index).Select(b => b.Delegate.Address).ToHashSet();
                    foreach (var ua in repo.GetUserDelegates(true).Where(a => a.User.VotingNotify))
                    {
                        if (!ballots.Contains(ua.Address))
                        {
                            var result = resMgr.Get(Res.DelegateDidNotVoted, (ua, p));
                            if (!ua.User.HideHashTags)
                                result += "\n\n#proposal" + p.HashTag() + ua.HashTag();
                            SendTextMessage(ua.UserId, result, ReplyKeyboards.MainMenu(resMgr, ua.User));
                        }
					}
				}

                if (prevPeriod.kind == "exploration" && period.kind == "proposal")
				{
                    var proposal = tzKtClient.GetProposals(prevPeriod.epoch).OrderByDescending(p => p.rolls).First();
                    var p = repo.GetProposal(proposal.hash);
                    foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
					{
						var result = resMgr.Get(Res.TestingVoteFailed,
							new ContextObject { p = p, u = u, Block = block.Level, Period = prevPeriod.index });
						if (!u.HideHashTags)
							result += "\n\n#proposal" + p.HashTag();
						SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
					}
                }

                if (prevPeriod.kind == "promotion")
				{
                    var proposal = tzKtClient.GetProposals(prevPeriod.epoch).OrderByDescending(p => p.rolls).First();
                    var p = repo.GetProposal(proposal.hash);
                    foreach (var u in repo.GetUsers().Where(o => !o.Inactive && o.VotingNotify))
					{
						var result = period.kind == "adoption"
							? resMgr.Get(Res.PromotionVoteSuccess,
								new ContextObject { p = p, u = u, Block = block.Level, Period = prevPeriod.index })
							: resMgr.Get(Res.PromotionVoteFailed,
								new ContextObject { p = p, u = u, Block = prevPeriod.index });
						if (!u.HideHashTags)
							result += "\n\n#proposal" + p.HashTag();
						SendTextMessage(u.Id, result, ReplyKeyboards.MainMenu(resMgr, u));
					}
                }
            }
        }
	}
}
