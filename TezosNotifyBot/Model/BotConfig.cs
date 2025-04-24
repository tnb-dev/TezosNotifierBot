using System;
using System.Collections.Generic;

namespace TezosNotifyBot.Model
{
    public class BotConfig
    {
		public BotConfig()
		{
			CarthageStart = 851969;
		}
				
		public TelegramOptions Telegram { get; set; }

		public string TzKtUrl { get; set; }
		
        public string ProxyType { get; set; }
        public string ProxyAddress { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyLogin { get; set; }
        public string ProxyPassword { get; set; }
		public string UploadPath { get; set; }
		public int CarthageStart { get; set; }
		public int WhaleSeriesLength { get; set; }

		public string CryptoCompareToken { get; set; }

		public TimeSpan DelegateInactiveTime { get; set; }
	}

    public class TelegramOptions
    {
	    public string BotSecret { get; set; }
	    public List<string> DevUsers { get; set; }
	    public long[] ActivityChat { get; set; }
		public long VotingChat { get; set; }
	}
}
