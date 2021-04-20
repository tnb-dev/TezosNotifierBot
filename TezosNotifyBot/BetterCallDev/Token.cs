using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TezosNotifyBot.BetterCallDev
{
    public class Token
    {
        public string contract { get; set; }
        public string network { get; set; }
        public int token_id { get; set; }
        public string symbol { get; set; }
        public string name { get; set; }
        public int decimals { get; set; }
        public string balance { get; set; }
        public decimal Balance => (decimal)(BigInteger.Parse(balance) / BigInteger.Parse("1".PadRight(decimals + 1, '0')));
        public int level { get; set; }
    }
}
