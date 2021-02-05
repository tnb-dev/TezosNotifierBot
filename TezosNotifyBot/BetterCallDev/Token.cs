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
        public decimal Balance => (decimal)BigInteger.Parse(balance) / (decimal)Math.Pow(10, decimals);
        public int level { get; set; }
    }
}
