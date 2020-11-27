using System;

namespace TezosNotifyBot.Domain
{
    public class User
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
        public int NetworkIssueNotify { get; set; }
        public int Explorer { get; set; }

        public override string ToString()
        {
            return Firstname + " " + Lastname + (Username != "" ? " @" + Username : "");
        }

        public int WhaleThreshold
        {
            get { return WhaleAlertThreshold; }
        }

        public bool NetworkIssueNotified;
    }
}