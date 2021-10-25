using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Storage;
using TezosNotifyBot.Storage.Extensions;
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

        internal void LogMessage(Telegram.Bot.Types.Chat from, int messageId, string text, string data)
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
        internal void LogMessage(long fromId, int messageId, string text, string data)
        {
            RunIsolatedDb(db =>
            {
                var msg = new Message
                {
                    CallbackQueryData = data,
                    CreateDate = DateTime.Now,
                    FromUser = true,
                    UserId = fromId,
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

        internal void LogOutMessage(long to, int messageId, string text)
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

        public bool UserExists(long id)
        {
            lock (_dbLock)
                return _db.Set<User>().Any(x => x.Id == id);
        }

        public User GetUser(long id)
        {
            lock (_dbLock)
                return _db.Set<User>().SingleOrDefault(x => x.Id == id);
        }

        public User GetUser(string userName)
        {
            lock (_dbLock)
                return _db.Set<User>().SingleOrDefault(o => o.Username == userName);
        }

        public Token GetToken(string address)
        {
            lock (_dbLock)
                return _db.Set<Token>().FirstOrDefault(o => o.ContractAddress == address);
        }

        public List<UserAddress> GetUserAddresses(string addr)
        {
            lock (_dbLock)
                return _db.UserAddresses
                    .Include(x => x.User)
                    .Where(o => o.Address == addr && !o.IsDeleted && !o.User.Inactive).ToList();
        }

        public List<UserAddress> GetUserAddresses()
        {
            lock (_dbLock)
                return _db.UserAddresses.Where(o => !o.IsDeleted && !o.User.Inactive).ToList();
        }

        public List<UserAddress> GetDelegators()
        {
            lock (_dbLock)
                return _db.UserAddresses
                    .Where(o => !o.IsDeleted && !o.User.Inactive && !_db.Delegates.Any(d => d.Address == o.Address) &&
                                o.NotifyAwardAvailable).Include(o => o.User).ToList();
        }

        public UserAddress GetUserTezosAddress(long userId, string addr)
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

        public List<UserAddress> GetUserAddresses(long userId)
        {
            lock (_dbLock)
                return _db.UserAddresses
                    .Where(o => o.UserId == userId && !o.IsDeleted)
                    .OrderBy(x => x.CreateDate)
                    .ToList();
        }

        public UserAddress GetUserAddress(long userId, int addressId)
        {
            lock (_dbLock)
                return _db.UserAddresses.FirstOrDefault(x => x.UserId == userId && x.Id == addressId);
        }

        public List<UserAddress> GetUserDelegates(bool all = false)
        {
            lock (_dbLock)
                return _db.UserAddresses.Where(o => !o.IsDeleted && (o.NotifyCycleCompletion || o.NotifyBakingRewards || o.NotifyOutOfFreeSpace || all) && !o.User.Inactive)
                    .Join(_db.Delegates, o => o.Address, o => o.Address, (o, d) => o).Include(x => x.User).ToList();
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
                user = User.New(u.Id, "", u.Username, u.FirstName, u.LastName,
                    (u.LanguageCode ?? "").Length > 2 ? u.LanguageCode.Substring(0, 2) : "en", 0);

                lock (_dbLock)
                {
                    _db.Add(user);
                    _db.SaveChanges();
                }
            }

            return user;
        }
        public User GetUser(Telegram.Bot.Types.Chat c)
        {
            var user = _db.Set<User>().SingleOrDefault(x => x.Id == c.Id);
            if (user != null)
            {
                if (user.Title != c.Title)
                {
                    user.Title = c.Title;
                    
                    lock (_dbLock)
                    {
                        _db.SaveChanges();
                    }
                }
            }
            else
            {
                user = User.New(c.Id, c.Title, c.Username, "", "", "en", (int)c.Type);

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

        internal void UpdateUserAddress(UserAddress ua)
        {
            lock (_dbLock)
                _db.SaveChanges();
        }

        public UserAddress AddUserAddress(User user, string addr, decimal bal, string name, long chatId)
        {
            lock (_dbLock)
            {
                var ua = _db.UserAddresses.FirstOrDefault(o =>
                    o.Address == addr && o.UserId == user.Id && o.ChatId == chatId);
                if (ua == null)
                {
                    ua = new UserAddress();
                    ua.UserId = user.Id;
                    ua.Address = addr;
                    ua.CreateDate = DateTime.Now;
                    ua.NotifyBakingRewards = user.Type == 0;
                    ua.NotifyDelegatorsBalance = user.Type == 0;
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

        public void AddWhaleTransaction(string from, string to, int level, DateTime timeStamp, decimal amount, string op)
		{
            lock(_dbLock)
			{
                if (_db.KnownAddresses.Any(o => (o.Address == from || o.Address == to) && o.ExcludeWhaleAlert))
                    return;
                _db.Add(new WhaleTransaction
                {
                    Amount = amount,
                    FromAddress = from,
                    Level = level,
                    OpHash = op,
                    Timestamp = timeStamp,
                    ToAddress = to
                });
                _db.SaveChanges();
			}
		}
        public void AddWhaleTransactionNotify(int whaleTransactionId, long userId)
        {
            lock (_dbLock)
            {
                _db.Add(new WhaleTransactionNotify
                {
                    WhaleTransactionId = whaleTransactionId,
                    UserId = userId
                });
                _db.SaveChanges();
            }
        }

        public void CleanWhaleTransactions(DateTime minDate) 
        {
            lock (_dbLock)
			{
                _db.RemoveRange(_db.WhaleTransactions.Where(o => o.Timestamp < minDate));
                _db.SaveChanges();
			}
        }

        public UserAddress RemoveAddr(long userId, string v)
        {
            lock (_dbLock)
            {
                if (!int.TryParse(v, out var uaid))
                    return null;
                var ua = _db.UserAddresses.FirstOrDefault(o => o.UserId == userId && o.Id == uaid && !o.IsDeleted);
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

        public KnownAddress GetKnownAddress(string addr)
        {
            return RunIsolatedDb(db => { return db.Set<KnownAddress>().SingleOrDefault(x => x.Address == addr); });
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
                    d = new KnownAddress(addr, name);
                    _db.KnownAddresses.Add(d);
                }

                d.Name = name;
                _db.SaveChanges();
            }
        }

        public List<KnownAddress> GetKnownAddresses()
        {
            return RunIsolatedDb(db => db.KnownAddresses.OrderBy(o => o.Name).ToList());
        }

        public List<Delegate> GetDelegates()
        {
            return RunIsolatedDb(db => db.Delegates.OrderBy(o => o.Name).ToList());
        }

        public List<WhaleTransaction> GetWhaleTransactions()
		{
            return RunIsolatedDb(db => 
            {
                return db.WhaleTransactions.Include(o => o.Notifications).ToList();
            });
		}

        /*internal void AddProposalVote(Proposal p, string from, int votingPeriod, int level, int ballot)
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
        }*/
                
        public Proposal GetProposal(string hash)
        {
            return RunIsolatedDb(db => { return db.Proposals.FirstOrDefault(o => o.Hash == hash); });
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
        /*
        internal List<string> GetProposalVotes(string hash, int period)
        {
            return RunIsolatedDb(db =>
            {
                return db.ProposalVotes.Where(o => o.Proposal.Hash == hash && o.VotingPeriod == period)
                    .Select(o => o.Delegate.Address).ToList();
            });
        }*/

        public void SaveMessage(Message message)
        {
            lock (_dbLock)
            {
                _db.Add(message);
                _db.SaveChanges();
            }
        }

        public AddressConfig GetAddressConfig(string address)
        {
            lock (_dbLock)
                return _db.Set<AddressConfig>().AsNoTracking().FirstOrDefault(x => x.Id == address);
        }

        public bool IsPayoutAddress(string address)
        {
            return RunIsolatedDb(db =>
            {
                return db.Set<KnownAddress>().Any(x =>
                           x.Address == address &&
                           EF.Functions.ILike(x.Name, "%payout%")) ||
                       IsDelegate(address);
            });
        }
    }
}