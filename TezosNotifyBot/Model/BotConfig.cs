using System;
using System.Collections.Generic;
using System.Text;

namespace NornPool.Model
{
    public class BotConfig
    {
		public BotConfig()
		{
			CarthageStart = 851969;
		}
        public string TelegramBotSecret { get; set; }
        public List<string> DevUserNames { get; set; }
        public string ProxyType { get; set; }
        public string ProxyAddress { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyLogin { get; set; }
        public string ProxyPassword { get; set; }
        public string DatabasePath { get; set; }
		public string UploadPath { get; set; }
		public int CarthageStart { get; set; }
		public long[] UserActivityChat { get; set; }

		public string TwitterConsumerKey { get; set; }
        public string TwitterConsumerKeySecret { get; set; }
        public string TwitterAccessToken { get; set; }
		public string TwitterAccessTokenSecret { get; set; }
		public long TwitterChatId { get; set; }
		public int TwitterNetworkIssueNotify { get; set; }
	}
}
