using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Query;

namespace TezosNotifyBot.Model
{
	public static class IQueryableExtensions
	{
		private static readonly TypeInfo QueryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();

		private static readonly FieldInfo QueryCompilerField = typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");

		private static readonly FieldInfo QueryModelGeneratorField = QueryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryModelGenerator");

		private static readonly FieldInfo DataBaseField = QueryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");

		private static readonly PropertyInfo DatabaseDependenciesField = typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");

		public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
		{
			var queryCompiler = (QueryCompiler)QueryCompilerField.GetValue(query.Provider);
			var modelGenerator = (QueryModelGenerator)QueryModelGeneratorField.GetValue(queryCompiler);
			var queryModel = modelGenerator.ParseQuery(query.Expression);
			var database = (IDatabase)DataBaseField.GetValue(queryCompiler);
			var databaseDependencies = (DatabaseDependencies)DatabaseDependenciesField.GetValue(database);
			var queryCompilationContext = databaseDependencies.QueryCompilationContextFactory.Create(false);
			var modelVisitor = (RelationalQueryModelVisitor)queryCompilationContext.CreateQueryModelVisitor();
			modelVisitor.CreateQueryExecutor<TEntity>(queryModel);
			var sql = modelVisitor.Queries.First().ToString();

			return sql;
		}
	}
	public class Repository
    {
        Dictionary<int, User> users;
        BotDataContext dc;
        LastBlock lastBlock;
        public Repository()
        {
            dc = new BotDataContext();
            dc.Database.Migrate();
            users = dc.Users.ToDictionary(o => o.UserId, o => o);
        }

        public List<User> GetUsers()
        {
            return users.Values.ToList();
        }

        internal void LogMessage(Telegram.Bot.Types.User from, int messageId, string text, string data)
        {
            lock (dc)
                using (var tmpDc = new BotDataContext())
                {
                    var msg = new Message
                    {
                        CallbackQueryData = data,
                        CreateDate = DateTime.Now,
                        FromUser = true,
                        UserId = GetUser(from).UserId,
                        TelegramMessageId = messageId,
                        Text = text
                    };
                    tmpDc.Add(msg);
                    tmpDc.SaveChanges();
                }
        }

		internal Message GetMessage(int messageId)
		{
			lock (dc)
				return dc.Messages.FirstOrDefault(o => o.MessageId == messageId);
		}

		internal void LogOutMessage(int to, int messageId, string text)
        {
            lock (dc)
                using (var tmpDc = new BotDataContext())
                {
                    var msg = new Message
                    {
                        CreateDate = DateTime.Now,
                        FromUser = false,
                        UserId = to,
                        TelegramMessageId = messageId,
                        Text = text
                    };
                    tmpDc.Add(msg);
                    tmpDc.SaveChanges();
                }
        }

        public (int,int,string) GetLastBlockLevel()
        {
            lock (dc)
            {
                if (lastBlock == null)
                    lastBlock = dc.LastBlock.SingleOrDefault();
                if (lastBlock == null)
                {
                    lastBlock = new LastBlock { Level = 0 };
                    dc.LastBlock.Add(lastBlock);
                    dc.SaveChanges();
                }
                return (lastBlock.Level, lastBlock.Priority, lastBlock.Hash);
            }
        }

        public void SetLastBlockLevel(int level, int priority, string hash)
        {
            GetLastBlockLevel();
            lastBlock.Level = level;
            lastBlock.Priority = priority;
			lastBlock.Hash = hash;
            lock (dc)
                dc.SaveChanges();
        }

        public bool UserExists(int id)
        {
            return users.ContainsKey(id);
        }

        public User GetUser(int id)
        {
            return users[id];
        }

        public User GetUser(string userName)
        {
            return users.Values.FirstOrDefault(o => o.Username == userName);
        }

        public List<UserAddress> GetUserAddresses(string addr)
        {
            lock (dc)
                return dc.UserAddresses.Where(o => o.Address == addr && !o.IsDeleted && !o.User.Inactive).ToList();
		}

		public List<UserAddress> GetUserAddresses()
		{
			lock (dc)
				return dc.UserAddresses.Where(o => !o.IsDeleted && !o.User.Inactive).ToList();
		}

		public UserAddress GetUserTezosAddress(int userId, string addr)
		{
			lock (dc)
			{
				var ua = dc.UserAddresses.FirstOrDefault(o => o.Address == addr && !o.IsDeleted && o.UserId == userId);
				if (ua != null)
					return ua;
				var d = dc.Delegates.FirstOrDefault(o => o.Address == addr);
				if (d != null && !String.IsNullOrEmpty(d.Name))
					return new UserAddress { Address = addr, Name = d.Name };
				var ka = dc.KnownAddresses.FirstOrDefault(o => o.Address == addr);
				if (ka != null)
					return new UserAddress { Address = addr, Name = ka.Name };
				return new UserAddress { Address = addr };
			}
		}

		public void DeleteTwitterMessage(TwitterMessage twitterMessage)
		{
			lock (dc)
			{
				dc.Remove(twitterMessage);
				dc.SaveChanges();
			}
		}

		public List<TwitterMessage> GetTwitterMessages(DateTime minCreateDate)
		{
			lock (dc)
				return dc.TwitterMessages.Where(o => o.CreateDate >= minCreateDate).OrderBy(o => o.TwitterMessageID).ToList();
		}

		public TwitterMessage GetTwitterMessage(int twitterMessageId)
		{
			lock (dc)
				return dc.TwitterMessages.Single(o => o.TwitterMessageID == twitterMessageId);
		}

		public TwitterMessage CreateTwitterMessage(string text)
		{
			lock (dc)
			{
				var twm = new TwitterMessage { Text = text, CreateDate = DateTime.Now };
				dc.Add(twm);
				dc.SaveChanges();
				return twm;
			}
		}

		public void UpdateTwitterMessage(TwitterMessage twitterMessage)
		{
			lock(dc)
			{
				dc.SaveChanges();
			}
		}

		public List<UserAddress> GetUserAddresses(int userId)
        {
            lock (dc)
                return dc.UserAddresses.Where(o => o.UserId == userId && !o.IsDeleted).ToList();
        }

        public List<UserAddress> GetUserDelegates()
        {
            lock (dc)
                return dc.UserAddresses.Where(o => !o.IsDeleted && o.NotifyCycleCompletion && !o.User.Inactive).Join(dc.Delegates, o => o.Address, o => o.Address, (o, d) => o).ToList();
        }

        public User GetUser(Telegram.Bot.Types.User u)
        {
            User res;
            if (users.ContainsKey(u.Id))
            {
                res = users[u.Id];
                if (res.Username != u.Username ||
                    res.Lastname != u.LastName ||
                    res.Firstname != u.FirstName)
                {
                    lock (users)
                    {
                        res.Username = u.Username;
                        res.Lastname = u.LastName;
                        res.Firstname = u.FirstName;
                        lock (dc)
                            dc.SaveChanges();
                    }
                }
                return res;
            }
            lock (users)
            {
                if (users.ContainsKey(u.Id))
                    return users[u.Id];
				res = new User
				{
					CreateDate = DateTime.Now,
					Firstname = u.FirstName,
					Lastname = u.LastName,
					UserId = u.Id,
					Username = u.Username,
					Language = (u.LanguageCode ?? "").Length > 2 ? u.LanguageCode.Substring(0, 2) : "en",
					WhaleAlertThreshold = 500000,
					VotingNotify = true
				};
                lock (dc)
                {
                    dc.Add(res);
                    dc.SaveChanges();
                }
                users.Add(u.Id, res);
            }
            return res;
        }

        public List<string[]> RunSql(string sql)
        {
            lock (dc)
            {
                var conn = dc.Database.GetDbConnection();
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                List<string[]> result = new List<string[]>();
                try
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.HasRows)
                        {
                            result.Add(new string[] { dr.RecordsAffected.ToString() + " records affected" });
                            return result;
                        }
                        string[] data = new string[dr.FieldCount];
                        for (int i = 0; i < data.Length; i++)
                            data[i] = dr.GetName(i);
                        result.Add(data);
                        while (dr.Read())
                        {
                            data = new string[dr.FieldCount];
                            for (int i = 0; i < data.Length; i++)
                                data[i] = dr.GetValue(i).ToString();
                            result.Add(data);
                        }
                    }
                    return result;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        internal void UpdateBalance(UserAddress ua)
        {
            lock (dc)
                dc.SaveChanges();
        }

        public UserAddress AddUserAddress(int userId, string addr, decimal bal, string name, long chatId)
        {
            lock (dc)
            {
                var ua = dc.UserAddresses.FirstOrDefault(o => o.Address == addr && o.UserId == userId && o.ChatId == chatId);
                if (ua == null)
                {
                    ua = new UserAddress();
                    ua.UserId = userId;
                    ua.Address = addr;
                    ua.CreateDate = DateTime.Now;
                    ua.NotifyBakingRewards = true;
					ua.ChatId = chatId;
                    dc.Add(ua);
                }
                ua.IsDeleted = false;
                ua.LastUpdate = DateTime.Now;
                ua.Balance = bal;
                ua.Name = name;
                ua.AmountThreshold = 0;
                dc.SaveChanges();
                return ua;
            }
        }

        public UserAddress RemoveAddr(int id, string v)
        {
            lock (dc)
            {
                if (!int.TryParse(v, out int uaid))
                    return null;
                var ua = dc.UserAddresses.FirstOrDefault(o => o.UserId == id && o.UserAddressId == uaid && !o.IsDeleted);
                if (ua == null)
                    return null;
                ua.IsDeleted = true;
                dc.SaveChanges();
                return ua;
            }
        }

        internal bool IsDelegate(string addr)
        {
            lock (dc)
                return dc.Delegates.Any(o => o.Address == addr);
        }

        internal void AddDelegate(string addr, string name)
        {
            lock (dc)
            {
                var d = new Delegate
                {
                    Address = addr,
                    Name = name
                };
                dc.Add(d);
                dc.SaveChanges();
            }
        }

        internal void UpdateUser(User u)
        {
            lock (dc)
                dc.SaveChanges();
        }

        internal string GetDelegateName(string addr)
        {
            lock (dc)
            {
                var d = dc.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d != null && !String.IsNullOrEmpty(d.Name))
                    return d.Name;
                return addr.ShortAddr();
            }
		}

		internal string GetKnownAddressName(string addr)
		{
			lock (dc)
			{
				var ka = dc.KnownAddresses.FirstOrDefault(o => o.Address == addr);
				if (ka != null && !String.IsNullOrEmpty(ka.Name))
					return ka.Name;
				return null;
			}
		}

		internal void UpdateDelegate(Delegate d)
        {
            lock (dc)
                dc.SaveChanges();
        }

        public Delegate GetOrCreateDelegate(string addr)
        {
            lock (dc)
            {
                var d = dc.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new Delegate { Address = addr };
                    dc.Delegates.Add(d);
					dc.SaveChanges();
                }
                return d;
            }
        }

        internal void SetDelegateName(string addr, string name)
        {
            lock (dc)
            {
                var d = dc.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new Delegate { Address = addr };
                    dc.Delegates.Add(d);
                }
                d.Name = name;
                dc.SaveChanges();
            }
		}

		internal void SetKnownAddress(string addr, string name)
		{
			lock (dc)
			{
				var d = dc.KnownAddresses.FirstOrDefault(o => o.Address == addr);
				if (d == null)
				{
					d = new KnownAddress { Address = addr };
					dc.KnownAddresses.Add(d);
				}
				d.Name = name;
				dc.SaveChanges();
			}
		}

		public List<KnownAddress> GetKnownAddresses()
		{
			lock (dc)
				using (var tmpDc = new BotDataContext())
					return tmpDc.KnownAddresses.OrderBy(o => o.Name).ToList();
		}

		public List<Delegate> GetDelegates()
		{
			lock (dc)
				using (var tmpDc = new BotDataContext())
					return tmpDc.Delegates.OrderBy(o => o.Name).ToList();
		}

		public void UpdateDelegateRewards1(string addr, int cycle, long addPlan, long addAcc)
        {
            lock (dc)
                using (var tmpDc = new BotDataContext())
                {
                    var d = tmpDc.Delegates.FirstOrDefault(o => o.Address == addr);
                    if (d == null)
                    {
                        d = new Delegate { Address = addr };
                        tmpDc.Delegates.Add(d);
                    }
                    var dr = tmpDc.DelegateRewards.FirstOrDefault(o => o.Cycle == cycle && o.DelegateId == d.DelegateId);
                    if (dr == null)
                    {
                        dr = new DelegateRewards { Cycle = cycle };
                        dr.Delegate = d;
                        tmpDc.DelegateRewards.Add(dr);
                    }
                    dr.Rewards = dr.Rewards + addPlan;
                    dr.Accured = dr.Accured + addAcc;
                    tmpDc.SaveChanges();
                }
        }

        public void UpdateDelegateAccured(string addr, int cycle, long accured)
        {
            lock (dc)
                using (var tmpDc = new BotDataContext())
                {
                    var d = tmpDc.Delegates.FirstOrDefault(o => o.Address == addr);
                    if (d == null)
                    {
                        d = new Delegate { Address = addr };
                        tmpDc.Delegates.Add(d);
                    }
                    var dr = tmpDc.DelegateRewards.FirstOrDefault(o => o.Cycle == cycle && o.DelegateId == d.DelegateId);
                    if (dr == null)
                    {
                        dr = new DelegateRewards { Cycle = cycle };
                        dr.Delegate = d;
                        tmpDc.DelegateRewards.Add(dr);
                    }
                    dr.Accured = accured;
                    tmpDc.SaveChanges();
                }
        }

		internal void AddProposalVote(Proposal p, string from, int votingPeriod, int level, int ballot)
		{
			var d = GetOrCreateDelegate(from);
			lock (dc)
				using (var tmpDc = new BotDataContext())
				{
					var pv = new ProposalVote();
					pv.ProposalID = p.ProposalID;
					pv.DelegateId = d.DelegateId;
					pv.Level = level;
					pv.Ballot = ballot;
					pv.VotingPeriod = votingPeriod;
					tmpDc.ProposalVotes.Add(pv);					
					tmpDc.SaveChanges();
				}
		}

		public DelegateRewards GetDelegateRewards1(string addr, int cycle)
        {
            lock (dc)
                using (var tmpDc = new BotDataContext())
                    return tmpDc.DelegateRewards.Where(o => o.Cycle == cycle && o.Delegate.Address == addr).FirstOrDefault() ?? new DelegateRewards();
        }
        public List<DelegateRewards> GetLastDelegateRewards1(string addr, int cycles)
        {
            lock (dc)
                using (var tmpDc = new BotDataContext())
                    return tmpDc.DelegateRewards.Where(o => o.Delegate.Address == addr).OrderByDescending(o => o.Cycle).Skip(1).Take(cycles).ToList();
        }

		public Proposal GetProposal(string hash)
		{
			lock (dc)
				using (var tmpDc = new BotDataContext())
					return tmpDc.Proposals.Where(o => o.Hash == hash).FirstOrDefault();
		}

		public Proposal AddProposal(string hash, string addr, int votingPeriod)
		{
			lock (dc)
				using (var tmpDc = new BotDataContext())
				{
					var d = tmpDc.Delegates.FirstOrDefault(o => o.Address == addr);
					if (d == null)
					{
						d = new Delegate { Address = addr };
						tmpDc.Delegates.Add(d);
					}
					var p = new Proposal
					{
						Delegate = d,
						Period = votingPeriod,
						Hash = hash,
						Name = hash.Substring(0, 7) + "…" + hash.Substring(hash.Length - 5)
					};
					tmpDc.Proposals.Add(p);
					tmpDc.SaveChanges();
					return p;
				}
		}

		internal List<string> GetProposalVotes(string hash, int period)
		{
			lock (dc)
				using (var tmpDc = new BotDataContext())
					return tmpDc.ProposalVotes.Where(o => o.Proposal.Hash == hash && o.VotingPeriod == period).Select(o => o.Delegate.Address).ToList();
		}

		public void SaveBakingEndorsingRights(Tezos.BakingRights[] br, Tezos.EndorsingRights[] er)
		{
			using (var tmpDc = new BotDataContext())
			{
				if (tmpDc.BakingRights.Any(o => o.Level == br[0].level))
					return;
				var delegates = tmpDc.Delegates.Where(o => o.Address != null).ToList().ToDictionary(o => o.Address, o => o);
				Func<string, Delegate> getDelegate = addr =>
				{
					if (delegates.ContainsKey(addr))
						return delegates[addr];
					var d = new Delegate { Address = addr };
					tmpDc.Delegates.Add(d);
					delegates[addr] = d;
					return d;
				};
				foreach(var b in br)
				{
					tmpDc.BakingRights.Add(new BakingRights
					{
						Delegate = getDelegate(b.@delegate),
						Level = b.level
					});
				}
				foreach(var e in er)
				{
					tmpDc.EndorsingRights.Add(new EndorsingRights
					{
						Delegate = getDelegate(e.@delegate),
						Level = e.level,
						SlotCount = e.slots.Count
					});
				}
				tmpDc.SaveChanges();
			}
		}
		public void AddBalanceUpdate(string @delegate, int type, int level, long amount, int slots)
		{
			using (var tmpDc = new BotDataContext())
			{
				var d = tmpDc.Delegates.FirstOrDefault(o => o.Address == @delegate);
				if (d == null)
				{
					d = new Delegate { Address = @delegate };
					tmpDc.Delegates.Add(d);
				}

				var d_id = d.DelegateId;
				IQueryable<BalanceUpdate> q = tmpDc.BalanceUpdates.Where(o => o.DelegateId == d_id && o.Type == type && o.Level == level);
				//var sqlQuery = q.ToSql();
				if (q.Any())
					return;

				tmpDc.BalanceUpdates.Add(new BalanceUpdate
				{
					Delegate = d,
					Type = type,
					Amount = amount,
					Level = level,
					Slots = slots
				});

				tmpDc.SaveChanges();
			}
		}
		public long GetRewards(string @delegate, int cycle, bool includeMissed)
		{
			int from = cycle * 4096 + 1;
			int to = from + 4095;
			using (var tmpDc = new BotDataContext())
			{
				if (!includeMissed)
					return tmpDc.BalanceUpdates.Where(o => o.Delegate.Address == @delegate && o.Level >= from && o.Level <= to && o.Type <= 2).Sum(o => o.Amount);
				else
				{
					var bu = tmpDc.BalanceUpdates.Where(o => o.Delegate.Address == @delegate && o.Level >= from && o.Level <= to && o.Type <= 4 && o.Type >= 1);
					return bu.Sum(o => o.Amount);
				}
			}
		}
		public Dictionary<string, long> GetRewards(int from, int to, bool includeMissed)
		{
			using (var tmpDc = new BotDataContext())
			{
				var fromType = includeMissed ? 1 : 0;
				var toType = includeMissed ? 4 : 2;
				return tmpDc.BalanceUpdates.Where(o => o.Level >= from && o.Level <= to && fromType <= o.Type && o.Type <= toType)
					.GroupBy(o => o.Delegate.Address).Select(o => new { @Delegate = o.Key, Amount = o.Sum(o1 => o1.Amount) }).ToDictionary(o => o.Delegate, o => o.Amount);				
			}
		}
		public bool IsRightsLoaded(int cycle)
		{
			int from = cycle * 4096 + 1;
			using (var tmpDc = new BotDataContext())
				return tmpDc.BakingRights.Any(o => o.Level == from);
		}
		public List<Tuple<string, int>> GetEndorsingRights(int level)
		{
			using (var tmpDc = new BotDataContext())
				return tmpDc.EndorsingRights.Where(o => o.Level == level).Select(o => new { o.Delegate.Address, o.SlotCount }).ToList().Select(o => new Tuple<string, int>(o.Address, o.SlotCount)).ToList();
		}
		public List<Tuple<string, int>> GetCycleBakingRights(int cycle)
		{
			int from = cycle * 4096 + 1;
			int to = from + 4095;
			using (var tmpDc = new BotDataContext())
				return tmpDc.BakingRights.Where(o => o.Level >= from && o.Level <= to).Select(o => new { o.Delegate.Address, o.Level }).ToList().Select(o => new Tuple<string, int>(o.Address, o.Level)).ToList();
		}
		public List<BalanceUpdate> GetBalanceUpdates(string @delegate, int cycle)
		{
			int from = cycle * 4096 + 1;
			int to = from + 4095;
			using (var tmpDc = new BotDataContext())
				return tmpDc.BalanceUpdates.Where(o => o.Delegate.Address == @delegate && o.Level >= from && o.Level <= to).ToList();
		}
	}
}
