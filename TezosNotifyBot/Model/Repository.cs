using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Storage;
using BakingRights = TezosNotifyBot.Tezos.BakingRights;
using Delegate = TezosNotifyBot.Domain.Delegate;
using EndorsingRights = TezosNotifyBot.Tezos.EndorsingRights;

namespace TezosNotifyBot.Model
{
    public class Repository
    {
        private readonly DbContextOptions _dbOptions;
        private readonly TezosDataContext _db;

        private readonly object _dbLock = new object();

        private LastBlock lastBlock;

        public Repository(DbContextOptions dbOptions)
        {
            _dbOptions = dbOptions;
            _db = new TezosDataContext(dbOptions);
        }

        public List<User> GetUsers()
        {
            lock (_dbLock)
                return _db.Set<User>().ToList();
        }

        /// <summary>
        /// It's a awful trick helper. We will remove it's usages in feature releases
        /// </summary>
        [Obsolete]
        private void RunIsolatedDb(Action<TezosDataContext> action)
        {
            lock (_dbLock)
            {
                using var db = new TezosDataContext(_dbOptions);
                action(db);
            }
        }

        /// <summary>
        /// It's a awful trick helper. We will remove it's usages in feature releases
        /// </summary>
        [Obsolete]
        private TOut RunIsolatedDb<TOut>(Func<TezosDataContext, TOut> action)
        {
            lock (_dbLock)
            {
                using var db = new TezosDataContext(_dbOptions);
                return action(db);
            }
        }
        
        internal void LogMessage(Telegram.Bot.Types.User from, int messageId, string text, string data)
        {
            RunIsolatedDb(db =>
            {
                var msg = new Message
                {
                    CallbackQueryData = data,
                    CreateDate = DateTime.Now,
                    FromUser = true,
                    UserId = GetUser(from).Id,
                    TelegramMessageId = messageId,
                    Text = text
                };
                db.Add(msg);
                db.SaveChanges();
            });
        }

        internal Message GetMessage(int messageId)
        {
            lock (_dbLock)
                return _db.Messages.FirstOrDefault(o => o.Id == messageId);
        }

        internal void LogOutMessage(int to, int messageId, string text)
        {
            RunIsolatedDb(db =>
            {
                var msg = new Message
                {
                    CreateDate = DateTime.Now,
                    FromUser = false,
                    UserId = to,
                    TelegramMessageId = messageId,
                    Text = text
                };
                db.Add(msg);
                db.SaveChanges();
            });
        }

        public (int, int, string) GetLastBlockLevel()
        {
            lock (_dbLock)
            {
                if (lastBlock == null)
                    lastBlock = _db.LastBlock.SingleOrDefault();
                if (lastBlock == null)
                {
                    lastBlock = new LastBlock {Level = 0};
                    _db.LastBlock.Add(lastBlock);
                    _db.SaveChanges();
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

            lock (_dbLock)
                _db.SaveChanges();
        }

        public bool UserExists(int id)
        {
            lock (_dbLock)
                return _db.Set<User>().Any(x => x.Id == id);
        }

        public User GetUser(int id)
        {
            lock (_dbLock) 
                return _db.Set<User>().SingleOrDefault(x => x.Id == id);
        }

        public User GetUser(string userName)
        {
            lock (_dbLock)
                return _db.Set<User>().SingleOrDefault(o => o.Username == userName);
        }

        public List<UserAddress> GetUserAddresses(string addr)
        {
            lock (_dbLock)
                return _db.UserAddresses.Where(o => o.Address == addr && !o.IsDeleted && !o.User.Inactive).ToList();
        }

        public List<UserAddress> GetUserAddresses()
        {
            lock (_dbLock)
                return _db.UserAddresses.Where(o => !o.IsDeleted && !o.User.Inactive).ToList();
        }

        public UserAddress GetUserTezosAddress(int userId, string addr)
        {
            lock (_dbLock)
            {
                var ua = _db.UserAddresses.FirstOrDefault(o => o.Address == addr && !o.IsDeleted && o.UserId == userId);
                if (ua != null)
                    return ua;
                var d = _db.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d != null && !String.IsNullOrEmpty(d.Name))
                    return new UserAddress {Address = addr, Name = d.Name};
                var ka = _db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
                if (ka != null)
                    return new UserAddress {Address = addr, Name = ka.Name};
                return new UserAddress {Address = addr};
            }
        }

        public void DeleteTwitterMessage(TwitterMessage twitterMessage)
        {
            lock (_dbLock)
            {
                _db.Remove(twitterMessage);
                _db.SaveChanges();
            }
        }

        public List<TwitterMessage> GetTwitterMessages(DateTime minCreateDate)
        {
            lock (_dbLock)
                return _db.TwitterMessages.Where(o => o.CreateDate >= minCreateDate).OrderBy(o => o.Id).ToList();
        }

        public TwitterMessage GetTwitterMessage(int twitterMessageId)
        {
            lock (_dbLock)
                return _db.TwitterMessages.Single(o => o.Id == twitterMessageId);
        }

        public TwitterMessage CreateTwitterMessage(string text)
        {
            lock (_dbLock)
            {
                var twm = new TwitterMessage {Text = text, CreateDate = DateTime.Now};
                _db.Add(twm);
                _db.SaveChanges();
                return twm;
            }
        }

        public void UpdateTwitterMessage(TwitterMessage twitterMessage)
        {
            lock (_dbLock)
                _db.SaveChanges();
        }

        public List<UserAddress> GetUserAddresses(int userId)
        {
            lock (_dbLock)
                return _db.UserAddresses.Where(o => o.UserId == userId && !o.IsDeleted).ToList();
        }

        public UserAddress GetUserAddress(int userId, int addressId)
        {
            lock (_dbLock)
                return _db.UserAddresses.FirstOrDefault(x => x.UserId == userId && x.Id == addressId);
        }
        
        public List<UserAddress> GetUserDelegates()
        {
            lock (_dbLock)
                return _db.UserAddresses.Where(o => !o.IsDeleted && o.NotifyCycleCompletion && !o.User.Inactive)
                    .Join(_db.Delegates, o => o.Address, o => o.Address, (o, d) => o).ToList();
        }

        public User GetUser(Telegram.Bot.Types.User u)
        {
            var user = _db.Set<User>().SingleOrDefault(x => x.Id == u.Id);
            if (user != null)
            {
                if (user.Username != u.Username ||
                    user.Lastname != u.LastName ||
                    user.Firstname != u.FirstName)
                {
                    user.Username = u.Username;
                    user.Lastname = u.LastName;
                    user.Firstname = u.FirstName;
                    
                    lock (_dbLock)
                    {
                        _db.SaveChanges();
                    }
                }
            }
            else
            {
                user = User.New(u.Id, u.Username, u.FirstName, u.LastName,
                    (u.LanguageCode ?? "").Length > 2 ? u.LanguageCode.Substring(0, 2) : "en");

                lock (_dbLock)
                {
                    _db.Add(user);
                    _db.SaveChanges();
                }
            }

            return user;
        }

        public List<string[]> RunSql(string sql)
        {
            lock (_dbLock)
            {
                var conn = _db.Database.GetDbConnection();
            
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                var result = new List<string[]>();
                try
                {
                    conn.Open();
                    using var reader = cmd.ExecuteReader();
                
                    if (reader.HasRows is false)
                    {
                        result.Add(new[] {$"{reader.RecordsAffected} records affected"});
                        return result;
                    }

                    var data = new string[reader.FieldCount];
                    for (var i = 0; i < data.Length; i++)
                        data[i] = reader.GetName(i);
                
                    result.Add(data);
                    while (reader.Read())
                    {
                        data = new string[reader.FieldCount];
                        for (var i = 0; i < data.Length; i++)
                            data[i] = reader.GetValue(i).ToString();
                        result.Add(data);
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
            lock (_dbLock)
                _db.SaveChanges();
        }

        public UserAddress AddUserAddress(int userId, string addr, decimal bal, string name, long chatId)
        {
            lock (_dbLock)
            {
                var ua = _db.UserAddresses.FirstOrDefault(o =>
                    o.Address == addr && o.UserId == userId && o.ChatId == chatId);
                if (ua == null)
                {
                    ua = new UserAddress();
                    ua.UserId = userId;
                    ua.Address = addr;
                    ua.CreateDate = DateTime.Now;
                    ua.NotifyBakingRewards = true;
                    ua.ChatId = chatId;
                    _db.Add(ua);
                }

                ua.IsDeleted = false;
                ua.LastUpdate = DateTime.Now;
                ua.Balance = bal;
                ua.Name = name;
                ua.AmountThreshold = 0;
                _db.SaveChanges();
                return ua;
            }
        }

        public UserAddress RemoveAddr(int id, string v)
        {
            lock (_dbLock)
            {
                if (!int.TryParse(v, out var uaid))
                    return null;
                var ua = _db.UserAddresses.FirstOrDefault(o => o.UserId == id && o.Id == uaid && !o.IsDeleted);
                if (ua == null)
                    return null;
                ua.IsDeleted = true;
                _db.SaveChanges();
                return ua;
            }
        }

        internal bool IsDelegate(string addr)
        {
            lock (_dbLock)
                return _db.Delegates.Any(o => o.Address == addr);
        }

        internal void AddDelegate(string addr, string name)
        {
            var d = new Delegate
            {
                Address = addr,
                Name = name
            };
            
            lock (_dbLock)
            {
                _db.Add(d);
                _db.SaveChanges();
            }
        }

        internal void UpdateUser(User u)
        {
            lock (_dbLock)
                _db.SaveChanges();
        }

        internal string GetDelegateName(string addr)
        {
            lock (_dbLock)
            {
                var d = _db.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d != null && !String.IsNullOrEmpty(d.Name))
                    return d.Name;
                return addr.ShortAddr();
            }
        }

        internal string GetKnownAddressName(string addr)
        {
            return RunIsolatedDb(db =>
            {
                var ka = db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
                if (ka != null && !String.IsNullOrEmpty(ka.Name))
                    return ka.Name;
                return null;
            });
        }

        internal void UpdateDelegate(Delegate d)
        {
            lock (_dbLock)
                _db.SaveChanges();
        }

        public Delegate GetOrCreateDelegate(string addr)
        {
            lock (_dbLock)
            {
                var d = _db.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new Delegate {Address = addr};
                    _db.Delegates.Add(d);
                    _db.SaveChanges();
                }

                return d;
            }
        }

        internal void SetDelegateName(string addr, string name)
        {
            lock (_dbLock)
            {
                var d = _db.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new Delegate {Address = addr};
                    _db.Delegates.Add(d);
                }

                d.Name = name;
                _db.SaveChanges();
            }
        }

        internal void SetKnownAddress(string addr, string name)
        {
            lock (_dbLock)
            {
                var d = _db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new KnownAddress {Address = addr};
                    _db.KnownAddresses.Add(d);
                }

                d.Name = name;
                _db.SaveChanges();
            }
        }

        public List<KnownAddress> GetKnownAddresses()
        {
            return RunIsolatedDb(db =>
            {
                return db.KnownAddresses.OrderBy(o => o.Name).ToList();
            });
        }

        public List<Delegate> GetDelegates()
        {
            return RunIsolatedDb(db =>
            {
                return db.Delegates.OrderBy(o => o.Name).ToList();
            });
        }

        public void UpdateDelegateRewards1(string addr, int cycle, long addPlan, long addAcc)
        {
            RunIsolatedDb(db =>
            {
                var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new Delegate {Address = addr};
                    db.Delegates.Add(d);
                }

                var dr = db.DelegateRewards.FirstOrDefault(o => o.Cycle == cycle && o.DelegateId == d.Id);
                if (dr == null)
                {
                    dr = new DelegateRewards {Cycle = cycle};
                    dr.Delegate = d;
                    db.DelegateRewards.Add(dr);
                }

                dr.Rewards = dr.Rewards + addPlan;
                dr.Accured = dr.Accured + addAcc;
                db.SaveChanges();
            });
        }

        public void UpdateDelegateAccured(string addr, int cycle, long accured)
        {
            RunIsolatedDb(db =>
            {
                var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new Delegate {Address = addr};
                    db.Delegates.Add(d);
                }

                var dr = db.DelegateRewards.FirstOrDefault(o => o.Cycle == cycle && o.DelegateId == d.Id);
                if (dr == null)
                {
                    dr = new DelegateRewards {Cycle = cycle};
                    dr.Delegate = d;
                    db.DelegateRewards.Add(dr);
                }

                dr.Accured = accured;
                db.SaveChanges();
            });
        }

        internal void AddProposalVote(Proposal p, string from, int votingPeriod, int level, int ballot)
        {
            var d = GetOrCreateDelegate(from);
            RunIsolatedDb(db =>
            {
                var pv = new ProposalVote
                {
                    ProposalID = p.Id,
                    DelegateId = d.Id,
                    Level = level,
                    Ballot = ballot,
                    VotingPeriod = votingPeriod
                };
                db.ProposalVotes.Add(pv);
                db.SaveChanges();
            });
        }

        public DelegateRewards GetDelegateRewards1(string addr, int cycle)
        {
            return RunIsolatedDb(db =>
            {
                return db.DelegateRewards
                    .FirstOrDefault(o => o.Cycle == cycle && o.Delegate.Address == addr) ?? new DelegateRewards();
            });
        }

        public List<DelegateRewards> GetLastDelegateRewards1(string addr, int cycles)
        {
            return RunIsolatedDb(db =>
            {
                return db.DelegateRewards.Where(o => o.Delegate.Address == addr).OrderByDescending(o => o.Cycle)
                    .Skip(1).Take(cycles).ToList();
            });
        }

        public Proposal GetProposal(string hash)
        {
            return RunIsolatedDb(db =>
            {
                return db.Proposals.FirstOrDefault(o => o.Hash == hash);
            });
        }

        public Proposal AddProposal(string hash, string addr, int votingPeriod)
        {
            return RunIsolatedDb(db =>
            {
                var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
                if (d == null)
                {
                    d = new Delegate {Address = addr};
                    db.Delegates.Add(d);
                }

                var p = new Proposal
                {
                    Delegate = d,
                    Period = votingPeriod,
                    Hash = hash,
                    Name = hash.Substring(0, 7) + "…" + hash.Substring(hash.Length - 5)
                };
                db.Proposals.Add(p);
                db.SaveChanges();
                return p;
            });
        }

        internal List<string> GetProposalVotes(string hash, int period)
        {
            return RunIsolatedDb(db =>
            {
                return db.ProposalVotes.Where(o => o.Proposal.Hash == hash && o.VotingPeriod == period)
                    .Select(o => o.Delegate.Address).ToList();
            });
        }

        public void SaveBakingEndorsingRights(BakingRights[] br, EndorsingRights[] er)
        {
            RunIsolatedDb(db =>
            {
                if (db.BakingRights.Any(o => o.Level == br[0].level))
                    return;
                var delegates = db.Delegates.Where(o => o.Address != null).ToList()
                    .ToDictionary(o => o.Address, o => o);
                Func<string, Delegate> getDelegate = addr =>
                {
                    if (delegates.ContainsKey(addr))
                        return delegates[addr];
                    var d = new Delegate {Address = addr};
                    db.Delegates.Add(d);
                    delegates[addr] = d;
                    return d;
                };
                foreach (var b in br)
                {
                    db.BakingRights.Add(new Domain.BakingRights
                    {
                        Delegate = getDelegate(b.@delegate),
                        Level = b.level
                    });
                }

                foreach (var e in er)
                {
                    db.EndorsingRights.Add(new Domain.EndorsingRights
                    {
                        Delegate = getDelegate(e.@delegate),
                        Level = e.level,
                        SlotCount = e.slots.Count
                    });
                }

                db.SaveChanges();
            });
        }

        public void AddBalanceUpdate(string @delegate, int type, int level, long amount, int slots)
        {
            RunIsolatedDb(db =>
            {
                var d = db.Delegates.FirstOrDefault(o => o.Address == @delegate);
                if (d == null)
                {
                    d = new Delegate {Address = @delegate};
                    db.Delegates.Add(d);
                }

                var d_id = d.Id;
                var q =
                    db.BalanceUpdates.Where(o => o.DelegateId == d_id && o.Type == type && o.Level == level);
                //var sqlQuery = q.ToSql();
                if (q.Any())
                    return;

                db.BalanceUpdates.Add(new BalanceUpdate
                {
                    Delegate = d,
                    Type = type,
                    Amount = amount,
                    Level = level,
                    Slots = slots
                });

                db.SaveChanges();
            });
        }

        public long GetRewards(string @delegate, int cycle, bool includeMissed)
        {
            var from = cycle * 4096 + 1;
            var to = from + 4095;

            return RunIsolatedDb(db =>
            {
                if (!includeMissed)
                    return db.BalanceUpdates.Where(o =>
                            o.Delegate.Address == @delegate && o.Level >= from && o.Level <= to && o.Type <= 2)
                        .Sum(o => o.Amount);

                var bu = db.BalanceUpdates.Where(o =>
                    o.Delegate.Address == @delegate && o.Level >= @from && o.Level <= to && o.Type <= 4 &&
                    o.Type >= 1);
                return bu.Sum(o => o.Amount);
            });
        }

        public Dictionary<string, long> GetRewards(int from, int to, bool includeMissed)
        {
            return RunIsolatedDb(db =>
            {
                var fromType = includeMissed ? 1 : 0;
                var toType = includeMissed ? 4 : 2;
                return db.BalanceUpdates.Where(o =>
                        o.Level >= from && o.Level <= to && fromType <= o.Type && o.Type <= toType)
                    .GroupBy(o => o.Delegate.Address)
                    .Select(o => new {@Delegate = o.Key, Amount = o.Sum(o1 => o1.Amount)})
                    .ToDictionary(o => o.Delegate, o => o.Amount);
            });
        }

        public bool IsRightsLoaded(int cycle)
        {
            var from = cycle * 4096 + 1;
            return RunIsolatedDb(db => db.BakingRights.Any(o => o.Level == from));
        }

        public List<Tuple<string, int>> GetEndorsingRights(int level)
        {
            return RunIsolatedDb(db =>
            {
                return db.EndorsingRights.Where(o => o.Level == level)
                    .Select(o => new {o.Delegate.Address, o.SlotCount}).ToList()
                    .Select(o => new Tuple<string, int>(o.Address, o.SlotCount)).ToList();
            });
        }

        public List<Tuple<string, int>> GetCycleBakingRights(int cycle)
        {
            var from = cycle * 4096 + 1;
            var to = from + 4095;
            return RunIsolatedDb(db =>
            {
                return db.BakingRights.Where(o => o.Level >= from && o.Level <= to)
                    .Select(o => new {o.Delegate.Address, o.Level}).ToList()
                    .Select(o => new Tuple<string, int>(o.Address, o.Level)).ToList();
            });
        }

        public List<BalanceUpdate> GetBalanceUpdates(string @delegate, int cycle)
        {
            var from = cycle * 4096 + 1;
            var to = from + 4095;
            return RunIsolatedDb(db =>
            {
                return db.BalanceUpdates
                    .Where(o => o.Delegate.Address == @delegate && o.Level >= from && o.Level <= to).ToList();
            });
        }

        public AddressConfig GetAddressConfig(string address)
        {
            return _db.Set<AddressConfig>().AsNoTracking().FirstOrDefault(x => x.Id == address);
        }
    }
}