using System;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Model;

namespace NornPool.Model
{
    public class BotConfig
    {
		public BotConfig()
		{
			CarthageStart = 851969;
		}

		public TwitterOptions Twitter { get; set; }
		
		public TelegramOptions Telegram { get; set; }

		public string TzKtUrl { get; set; }
		public string BetterCallDevUrl { get; set; }
		
        public string ProxyType { get; set; }
        public string ProxyAddress { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyLogin { get; set; }
        public string ProxyPassword { get; set; }
		public string UploadPath { get; set; }
		public int CarthageStart { get; set; }
		public int WhaleSeriesLength { get; set; }


		public Node[] Nodes { get; set; }

		public TimeSpan DelegateInactiveTime { get; set; }
		
		
		// TODO: Refactor access to this properties
		[Obsolete]
		public List<string> DevUserNames => Telegram.DevUsers;
		[Obsolete]
		public string TwitterConsumerKey => Twitter.ConsumerKey;
		[Obsolete]
        public string TwitterConsumerKeySecret => Twitter.ConsumerKeySecret;
        [Obsolete]
        public string TwitterAccessToken => Twitter.AccessToken;
        [Obsolete]
		public string TwitterAccessTokenSecret  => Twitter.AccessTokenSecret;
		[Obsolete]
		public long TwitterChatId => Twitter.ChatId;
		[Obsolete]
		public int TwitterNetworkIssueNotify => Twitter.NetworkIssueNotify;
	}

    public class TelegramOptions
    {
	    public string BotSecret { get; set; }
	    public List<string> DevUsers { get; set; }
	    public long[] ActivityChat { get; set; }
    }

    public class TwitterOptions
    {
	    public string ConsumerKey { get; set; }
	    public string ConsumerKeySecret { get; set; }
	    public string AccessToken { get; set; }
	    public string AccessTokenSecret { get; set; }
	    public long ChatId { get; set; }
	    public int NetworkIssueNotify { get; set; }
    }
}
