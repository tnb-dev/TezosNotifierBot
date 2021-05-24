using System;
using System.Collections.Generic;
using System.Numerics;

namespace TezosNotifyBot.BetterCallDev
{
    public class TokenBalance
    {
        public string contract { get; set; }
        public string network { get; set; }
        public int level { get; set; }
        public int token_id { get; set; }
        public string symbol { get; set; }
        public string name { get; set; }
        public int decimals { get; set; }
        public string description { get; set; }
        public string artifact_uri { get; set; }
        public string thumbnail_uri { get; set; }
        public string balance { get; set; }
        public decimal Balance => (decimal)(BigInteger.Parse(balance) / BigInteger.Parse("1".PadRight(decimals + 1, '0')));
    }
    public class Account
    {
        public IList<TokenBalance> balances { get; set; } = new List<TokenBalance>();
        public int total { get; set; }
    }
}
