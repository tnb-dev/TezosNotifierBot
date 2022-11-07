using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TezosNotifyBot.Shared.Extensions;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Storage;
using Microsoft.EntityFrameworkCore;

namespace TezosNotifyBot.Model
{
	public static class TezosDataContextExtensions
	{
        public static UserAddress GetUserTezosAddress(this TezosDataContext _db, long userId, string addr)
        {
            var ua = _db.UserAddresses.FirstOrDefault(o => o.Address == addr && !o.IsDeleted && o.UserId == userId);
            if (ua != null)
                return ua;
            var d = _db.Delegates.FirstOrDefault(o => o.Address == addr);
            if (d != null && !String.IsNullOrEmpty(d.Name))
                return new UserAddress { Address = addr, Name = d.Name };
            var ka = _db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
            if (ka != null)
                return new UserAddress { Address = addr, Name = ka.Name };
            return new UserAddress { Address = addr };
        }

        public static void CleanWhaleTransactions(this TezosDataContext _db, DateTime minDate)
        {
            _db.RemoveRange(_db.WhaleTransactions.Where(o => o.Timestamp < minDate));
            _db.SaveChanges();
        }

        public async static Task AddWhaleTransactionNotify(this TezosDataContext _db, int whaleTransactionId, long userId)
        {
            await _db.AddAsync(new WhaleTransactionNotify {
                WhaleTransactionId = whaleTransactionId,
                UserId = userId
            });
            await _db.SaveChangesAsync();
        }

        public static string GetDelegateName(this TezosDataContext _db, string addr)
        {
            var d = _db.Delegates.FirstOrDefault(o => o.Address == addr);
            if (d != null && !String.IsNullOrEmpty(d.Name))
                return d.Name;
            return addr.ShortAddr();
        }

        public static Proposal AddProposal(this TezosDataContext db, string hash, string addr, int votingPeriod)
        {
            var d = db.Delegates.FirstOrDefault(o => o.Address == addr);
            if (d == null)
            {
                d = new Domain.Delegate { Address = addr };
                db.Delegates.Add(d);
            }

            var p = new Proposal {
                Delegate = d,
                Period = votingPeriod,
                Hash = hash,
                Name = hash.Substring(0, 7) + "…" + hash.Substring(hash.Length - 5)
            };
            db.Proposals.Add(p);
            db.SaveChanges();
            return p;
        }

        public static (int, int, string) GetLastBlockLevel(this TezosDataContext db)
        {
            var lastBlock = db.LastBlock.SingleOrDefault();
            if (lastBlock == null)
            {
                lastBlock = new LastBlock { Level = 1 };
                db.LastBlock.Add(lastBlock);
                db.SaveChanges();
            }
            return (lastBlock.Level, lastBlock.Priority, lastBlock.Hash);
        }
        public static void SetLastBlockLevel(this TezosDataContext db, int level, int priority, string hash)
        {
            var lastBlock = db.LastBlock.SingleOrDefault() ?? new LastBlock();            
            lastBlock.Level = level;
            lastBlock.Priority = priority;
            lastBlock.Hash = hash;
            db.SaveChanges();
        }
        public static void AddWhaleTransaction(this TezosDataContext _db, string from, string to, int level, DateTime timeStamp, decimal amount, string op)
        {
            if (_db.KnownAddresses.Any(o => (o.Address == from || o.Address == to) && o.ExcludeWhaleAlert))
                return;
            _db.Add(new WhaleTransaction {
                Amount = amount,
                FromAddress = from,
                Level = level,
                OpHash = op,
                Timestamp = timeStamp,
                ToAddress = to
            });
            _db.SaveChanges();
        }
        public static List<UserAddress> GetUserAddresses(this TezosDataContext _db, string addr)
        {
            return _db.UserAddresses.Include(x => x.User).Where(o => o.Address == addr && !o.IsDeleted && !o.User.Inactive).ToList();
        }
        public static List<UserAddress> GetUserAddresses(this TezosDataContext _db, long userId)
        {
            return _db.UserAddresses.Where(o => o.UserId == userId && !o.IsDeleted).OrderBy(x => x.CreateDate).ToList();
        }
        public static bool IsPayoutAddress(this TezosDataContext db, string address)
        {
            return db.KnownAddresses.Any(x => x.Address == address && EF.Functions.ILike(x.Name, "%payout%")) || db.Delegates.Any(x => x.Address == address);
        }
        public static void LogMessage(this TezosDataContext db, long fromId, int messageId, string text, string data)
        {
            var msg = new Message {
                CallbackQueryData = data,
                CreateDate = DateTime.Now,
                FromUser = true,
                UserId = fromId,
                TelegramMessageId = messageId,
                Text = text
            };
            db.Add(msg);
            db.SaveChanges();
        }
        public static User GetUser(this TezosDataContext db, long id) => db.Users.SingleOrDefault(x => x.Id == id);
        public static List<User> GetVotingNotifyUsers(this TezosDataContext db) => db.Users.Where(o => !o.Inactive && o.VotingNotify).ToList();
        public static void LogMessage(this TezosDataContext db, Telegram.Bot.Types.User from, int messageId, string text, string data)
        {

            var msg = new Message {
                CallbackQueryData = data,
                CreateDate = DateTime.Now,
                FromUser = true,
                UserId = db.GetUser(from).Id,
                TelegramMessageId = messageId,
                Text = text
            };
            db.Add(msg);
            db.SaveChanges();
        }
        public static void LogMessage(this TezosDataContext db, Telegram.Bot.Types.Chat from, int messageId, string text, string data)
        {

            var msg = new Message {
                CallbackQueryData = data,
                CreateDate = DateTime.Now,
                FromUser = true,
                UserId = db.GetUser(from).Id,
                TelegramMessageId = messageId,
                Text = text
            };
            db.Add(msg);
            db.SaveChanges();
        }
        public static User GetUser(this TezosDataContext _db, Telegram.Bot.Types.User u)
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

                    _db.SaveChanges();
                }
            }
            else
            {
                user = User.New(u.Id, "", u.Username, u.FirstName, u.LastName, "en", 0);

                _db.Add(user);
                _db.SaveChanges();
            }

            return user;
        }
        public static User GetUser(this TezosDataContext _db, Telegram.Bot.Types.Chat c)
        {
            var user = _db.Set<User>().SingleOrDefault(x => x.Id == c.Id);
            if (user != null)
            {
                if (user.Title != c.Title)
                {
                    user.Title = c.Title;

                    _db.SaveChanges();
                }
            }
            else
            {
                user = User.New(c.Id, c.Title, c.Username, "", "", "en", (int)c.Type);

                _db.Add(user);
                _db.SaveChanges();
            }

            return user;
        }
        public static List<string[]> RunSql(this TezosDataContext _db, string sql)
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
                    result.Add(new[] { $"{reader.RecordsAffected} records affected" });
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

        public static string GetKnownAddressName(this TezosDataContext db, string addr)
        {
            var ka = db.KnownAddresses.FirstOrDefault(o => o.Address == addr);
            if (ka != null && !String.IsNullOrEmpty(ka.Name))
                return ka.Name;
            return null;
        }
        public static UserAddress AddUserAddress(this TezosDataContext _db, User user, string addr, decimal bal, string name, long chatId)
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
        public static void LogOutMessage(this TezosDataContext db, long to, int messageId, string text)
        {
            var msg = new Message {
                CreateDate = DateTime.Now,
                FromUser = false,
                UserId = to,
                TelegramMessageId = messageId,
                Text = text
            };
            db.Add(msg);
            db.SaveChanges();
        }
    }
}
