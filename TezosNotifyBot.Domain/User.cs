using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using TezosNotifyBot.Shared;
using TezosNotifyBot.Shared.Extensions;

namespace TezosNotifyBot.Domain
{
    public class User: IHasId<long>
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Username { get; set; }
        public DateTime CreateDate { get; set; }
        public string Language { get; set; }
        public bool Inactive { get; set; }
        public UserState UserState { get; set; }
        public int EditUserAddressId { get; set; }
        public bool HideHashTags { get; set; }
        public int WhaleAlertThreshold { get; set; }
        public bool VotingNotify { get; set; }
        public int Type { get; set; }

        public UserCurrency Currency { get; set; } = UserCurrency.Usd;

        public string CurrencyCode => Currency.GetDisplayName();
        
        /// <summary>
        /// Is user subscribed to notifications about tezos releases
        /// </summary>
        public bool ReleaseNotify { get; set; } = false;

        public int NetworkIssueNotify { get; set; }
        public int Explorer { get; set; } = 3;
        public bool SmartWhaleAlerts { get; set; }

        public CultureInfo Culture => new CultureInfo(Language);

        public override string ToString()
        {
            return (string.IsNullOrEmpty(Title) ? Firstname + " " + Lastname : Title) + (Username != "" ? " @" + Username : "");
        }

        public int WhaleThreshold
        {
            get { return WhaleAlertThreshold; }
        }

        public bool NetworkIssueNotified;

        private User()
        {
        }

        public static User New(long id, string Title, string username, string firstName, string lastName, string languageCode, int type)
        {
            return new User
            {
                CreateDate = DateTime.Now,
                Firstname = firstName,
                Lastname = lastName,
                Id = id,
                Username = username,
                Language = languageCode,
                // Enable release notifications only for new users
                ReleaseNotify = true,
                WhaleAlertThreshold = type == 0 ? 1000000 : 0,
                VotingNotify = true,
                Type = type,
                SmartWhaleAlerts = type == 0,
            };
        }
    }

    public enum UserCurrency
    {
        [Display(Name = "USD")]
        Usd = Currency.Usd,
        [Display(Name = "EUR")]
        Eur = Currency.Eur
    }
}