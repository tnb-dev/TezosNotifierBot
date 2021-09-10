using System;
using System.ComponentModel.DataAnnotations;
using TezosNotifyBot.Shared;
using TezosNotifyBot.Shared.Extensions;

namespace TezosNotifyBot.Domain
{
    public class User: IHasId<int>
    {
        public int Id { get; set; }
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

        public UserCurrency Currency { get; set; } = UserCurrency.Usd;

        public string CurrencyCode => Currency.GetDisplayName();
        
        /// <summary>
        /// Is user subscribed to notifications about tezos releases
        /// </summary>
        public bool ReleaseNotify { get; set; } = false;

        public int NetworkIssueNotify { get; set; }
        public int Explorer { get; set; }
        public bool SmartWhaleAlerts { get; set; }

        public override string ToString()
        {
            return Firstname + " " + Lastname + (Username != "" ? " @" + Username : "");
        }

        public int WhaleThreshold
        {
            get { return WhaleAlertThreshold; }
        }

        public bool NetworkIssueNotified;

        private User()
        {
        }

        public static User New(int id, string username, string firstName, string lastName, string languageCode)
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
                WhaleAlertThreshold = 500000,
                VotingNotify = true
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