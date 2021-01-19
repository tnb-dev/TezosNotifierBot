using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.BetterCallDev
{
    public class Account
    {
        public string address { get; set; }
        public string network { get; set; }
        public long balance { get; set; }
        public int tx_count { get; set; }
        public DateTime last_action { get; set; }
        public IList<Token> tokens { get; set; }
    }
}
