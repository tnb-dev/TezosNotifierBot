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
        private readonly TezosDataContext db;

        // TODO: This is potential memory leaks or mismatch with database
        private readonly Dictionary<int, User> users;
        private LastBlock lastBlock;

        public Repository(TezosDataContext db)
        {
            this.db = db;
            db.Database.Migrate();
            users = db.Users.ToDictionary(o => o.Id, o => o);
        }

        public List<User> GetUsers()
        {
            return users.Values.ToList();
        }

        internal void LogMessage(Telegram.Bot.Types.User from, int messageId, string text, string data)
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
        }

        internal Message GetMessage(int messageId)
        {
            return db.Messages.FirstOrDefault(o => o.Id == messageId);
        }

        internal void LogOutMessage(int to, int messageId, string text)
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
        }

        public (int, int, string) GetLastBlockLevel()
        {
            if (lastBlock == null)
                lastBlock = db.LastBlock.SingleOrDefault();
            if (lastBlock == null)
            {
                lastBlock = new LastBlock {Level = 0};
                db.LastBlock.Add(lastBlock);
                db.SaveChanges();
            }

            return (lastBlock.Level, lastBlock.Priority, lastBlock.Hash);
        }

        public void SetLastBlockLevel(int level, int priority, string hash)
        {
            GetLastBlockLevel();
            lastBlock.Level = level;
            lastBlock.Priority = priority;
            lastBlock.Hash = hash;
            db.SaveChanges();
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
            return db.UserAddresses.Where(o => o.Address == addr && !o.IsDeleted && !o.User.Inactive).ToList();
        }

        public List<UserAddress> GetUserAddresses()
        {
            return db.UserAddresses.Where(o => !o.IsDeleted && !o.User.Inactive).ToList();
        }

        public UserAddress GetUserTezosAddress(int userId, string addr)
        {
            var ua = db.UserAddresses.FirstOrDefault(o => o.Address == addr && !o.IsDeleted && o.UserId == userId);
            if (ua != null)
                return ua;
            var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
            if (d != null && !String.IsNullOrEmpty(d.Name))
                return new UserAddress {Address = addr, Name = d.Name};
            var ka = db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
            if (ka != null)
                return new UserAddress {Address = addr, Name = ka.Name};
            return new UserAddress {Address = addr};
        }

        public void DeleteTwitterMessage(TwitterMessage twitterMessage)
        {
            db.Remove(twitterMessage);
            db.SaveChanges();
        }

        public List<TwitterMessage> GetTwitterMessages(DateTime minCreateDate)
        {
            return db.TwitterMessages.Where(o => o.CreateDate >= minCreateDate).OrderBy(o => o.Id).ToList();
        }

        public TwitterMessage GetTwitterMessage(int twitterMessageId)
        {
            return db.TwitterMessages.Single(o => o.Id == twitterMessageId);
        }

        public TwitterMessage CreateTwitterMessage(string text)
        {
            var twm = new TwitterMessage {Text = text, CreateDate = DateTime.Now};
            db.Add(twm);
            db.SaveChanges();
            return twm;
        }

        public void UpdateTwitterMessage(TwitterMessage twitterMessage)
        {
            db.SaveChanges();
        }

        public List<UserAddress> GetUserAddresses(int userId)
        {
            return db.UserAddresses.Where(o => o.UserId == userId && !o.IsDeleted).ToList();
        }

        public List<UserAddress> GetUserDelegates()
        {
            return db.UserAddresses.Where(o => !o.IsDeleted && o.NotifyCycleCompletion && !o.User.Inactive)
                .Join(db.Delegates, o => o.Address, o => o.Address, (o, d) => o).ToList();
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
                        db.SaveChanges();
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
                    Id = u.Id,
                    Username = u.Username,
                    Language = (u.LanguageCode ?? "").Length > 2 ? u.LanguageCode.Substring(0, 2) : "en",
                    WhaleAlertThreshold = 500000,
                    VotingNotify = true
                };
                db.Add(res);
                db.SaveChanges();

                users.Add(u.Id, res);
            }

            return res;
        }

        public List<string[]> RunSql(string sql)
        {
            var conn = db.Database.GetDbConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = new List<string[]>();
            try
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.HasRows)
                    {
                        result.Add(new string[] {dr.RecordsAffected.ToString() + " records affected"});
                        return result;
                    }

                    var data = new string[dr.FieldCount];
                    for (var i = 0; i < data.Length; i++)
                        data[i] = dr.GetName(i);
                    result.Add(data);
                    while (dr.Read())
                    {
                        data = new string[dr.FieldCount];
                        for (var i = 0; i < data.Length; i++)
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

        internal void UpdateBalance(UserAddress ua)
        {
            db.SaveChanges();
        }

        public UserAddress AddUserAddress(int userId, string addr, decimal bal, string name, long chatId)
        {
            var ua = db.UserAddresses.FirstOrDefault(o =>
                o.Address == addr && o.UserId == userId && o.ChatId == chatId);
            if (ua == null)
            {
                ua = new UserAddress();
                ua.UserId = userId;
                ua.Address = addr;
                ua.CreateDate = DateTime.Now;
                ua.NotifyBakingRewards = true;
                ua.ChatId = chatId;
                db.Add(ua);
            }

            ua.IsDeleted = false;
            ua.LastUpdate = DateTime.Now;
            ua.Balance = bal;
            ua.Name = name;
            ua.AmountThreshold = 0;
            db.SaveChanges();
            return ua;
        }

        public UserAddress RemoveAddr(int id, string v)
        {
            if (!int.TryParse(v, out var uaid))
                return null;
            var ua = db.UserAddresses.FirstOrDefault(o => o.UserId == id && o.Id == uaid && !o.IsDeleted);
            if (ua == null)
                return null;
            ua.IsDeleted = true;
            db.SaveChanges();
            return ua;
        }

        internal bool IsDelegate(string addr)
        {
            return db.Delegates.Any(o => o.Address == addr);
        }

        internal void AddDelegate(string addr, string name)
        {
            var d = new Delegate
            {
                Address = addr,
                Name = name
            };
            db.Add(d);
            db.SaveChanges();
        }

        internal void UpdateUser(User u)
        {
            db.SaveChanges();
        }

        internal string GetDelegateName(string addr)
        {
            var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
            if (d != null && !String.IsNullOrEmpty(d.Name))
                return d.Name;
            return addr.ShortAddr();
        }

        internal string GetKnownAddressName(string addr)
        {
            var ka = db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
            if (ka != null && !String.IsNullOrEmpty(ka.Name))
                return ka.Name;
            return null;
        }

        internal void UpdateDelegate(Delegate d)
        {
            db.SaveChanges();
        }

        public Delegate GetOrCreateDelegate(string addr)
        {
            var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
            if (d == null)
            {
                d = new Delegate {Address = addr};
                db.Delegates.Add(d);
                db.SaveChanges();
            }

            return d;
        }

        internal void SetDelegateName(string addr, string name)
        {
            var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
            if (d == null)
            {
                d = new Delegate {Address = addr};
                db.Delegates.Add(d);
            }

            d.Name = name;
            db.SaveChanges();
        }

        internal void SetKnownAddress(string addr, string name)
        {
            var d = db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
            if (d == null)
            {
                d = new KnownAddress {Address = addr};
                db.KnownAddresses.Add(d);
            }

            d.Name = name;
            db.SaveChanges();
        }

        public List<KnownAddress> GetKnownAddresses()
        {
            return db.KnownAddresses.OrderBy(o => o.Name).ToList();
        }

        public List<Delegate> GetDelegates()
        {
            return db.Delegates.OrderBy(o => o.Name).ToList();
        }

        public void UpdateDelegateRewards1(string addr, int cycle, long addPlan, long addAcc)
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
        }

        public void UpdateDelegateAccured(string addr, int cycle, long accured)
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
        }

        internal void AddProposalVote(Proposal p, string from, int votingPeriod, int level, int ballot)
        {
            var d = GetOrCreateDelegate(from);
            var pv = new ProposalVote();
            pv.ProposalID = p.Id;
            pv.DelegateId = d.Id;
            pv.Level = level;
            pv.Ballot = ballot;
            pv.VotingPeriod = votingPeriod;
            db.ProposalVotes.Add(pv);
            db.SaveChanges();
        }

        public DelegateRewards GetDelegateRewards1(string addr, int cycle)
        {
            return db.DelegateRewards
                .FirstOrDefault(o => o.Cycle == cycle && o.Delegate.Address == addr) ?? new DelegateRewards();
        }

        public List<DelegateRewards> GetLastDelegateRewards1(string addr, int cycles)
        {
            return db.DelegateRewards.Where(o => o.Delegate.Address == addr).OrderByDescending(o => o.Cycle)
                .Skip(1).Take(cycles).ToList();
        }

        public Proposal GetProposal(string hash)
        {
            return db.Proposals.FirstOrDefault(o => o.Hash == hash);
        }

        public Proposal AddProposal(string hash, string addr, int votingPeriod)
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
        }

        internal List<string> GetProposalVotes(string hash, int period)
        {
            return db.ProposalVotes.Where(o => o.Proposal.Hash == hash && o.VotingPeriod == period)
                .Select(o => o.Delegate.Address).ToList();
        }

        public void SaveBakingEndorsingRights(BakingRights[] br, EndorsingRights[] er)
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
        }

        public void AddBalanceUpdate(string @delegate, int type, int level, long amount, int slots)
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
        }

        public long GetRewards(string @delegate, int cycle, bool includeMissed)
        {
            var from = cycle * 4096 + 1;
            var to = from + 4095;
            if (!includeMissed)
                return db.BalanceUpdates.Where(o =>
                        o.Delegate.Address == @delegate && o.Level >= from && o.Level <= to && o.Type <= 2)
                    .Sum(o => o.Amount);

            var bu = db.BalanceUpdates.Where(o =>
                o.Delegate.Address == @delegate && o.Level >= @from && o.Level <= to && o.Type <= 4 &&
                o.Type >= 1);
            return bu.Sum(o => o.Amount);
        }

        public Dictionary<string, long> GetRewards(int from, int to, bool includeMissed)
        {
            var fromType = includeMissed ? 1 : 0;
            var toType = includeMissed ? 4 : 2;
            return db.BalanceUpdates.Where(o =>
                    o.Level >= from && o.Level <= to && fromType <= o.Type && o.Type <= toType)
                .GroupBy(o => o.Delegate.Address)
                .Select(o => new {@Delegate = o.Key, Amount = o.Sum(o1 => o1.Amount)})
                .ToDictionary(o => o.Delegate, o => o.Amount);
        }

        public bool IsRightsLoaded(int cycle)
        {
            var from = cycle * 4096 + 1;
            return db.BakingRights.Any(o => o.Level == from);
        }

        public List<Tuple<string, int>> GetEndorsingRights(int level)
        {
            return db.EndorsingRights.Where(o => o.Level == level)
                .Select(o => new {o.Delegate.Address, o.SlotCount}).ToList()
                .Select(o => new Tuple<string, int>(o.Address, o.SlotCount)).ToList();
        }

        public List<Tuple<string, int>> GetCycleBakingRights(int cycle)
        {
            var from = cycle * 4096 + 1;
            var to = from + 4095;
            return db.BakingRights.Where(o => o.Level >= from && o.Level <= to)
                .Select(o => new {o.Delegate.Address, o.Level}).ToList()
                .Select(o => new Tuple<string, int>(o.Address, o.Level)).ToList();
        }

        public List<BalanceUpdate> GetBalanceUpdates(string @delegate, int cycle)
        {
            var from = cycle * 4096 + 1;
            var to = from + 4095;
            return db.BalanceUpdates
                .Where(o => o.Delegate.Address == @delegate && o.Level >= from && o.Level <= to).ToList();
        }
    }
}