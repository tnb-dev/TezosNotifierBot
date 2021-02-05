using System;
using System.Collections.Generic;

namespace TezosNotifyBot.BetterCallDev
{
    public class Account
    {
        public string address { get; set; }
        public string network { get; set; }
        public ulong balance { get; set; }
        public int tx_count { get; set; }
        public DateTime last_action { get; set; }
        public IList<Token> tokens { get; set; }
    }
}
