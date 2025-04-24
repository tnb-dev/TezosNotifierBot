using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TezosNotifyBot.Domain;

namespace TezosNotifyBot.Tezos
{
    public class MarketData
    {
        public decimal price_usd { get; set; }
		public decimal price_btc { get; set; }
        public decimal price_eur { get; set; }
        public DateTime Received { get; set; }
        
        public decimal CurrencyRate(Currency code) => code switch
        {
	        Currency.Eur => price_eur,
	        _ => price_usd,
        };
		
        public string CurrencyCode(string code) => code switch
        {
	        "eur" => "eur",
	        _ => "usd",
        };
    }

    public class FrozenBalanceByCycle
    {
        public int cycle { get; set; }
        public decimal deposit { get; set; }
        public decimal fees { get; set; }
        public decimal rewards { get; set; }
    }

    public class DelegateInfo
    {
        public decimal balance { get; set; }
        public decimal frozen_balance { get; set; }
        public List<FrozenBalanceByCycle> frozen_balance_by_cycle { get; set; }
        public decimal staking_balance { get; set; }
        public List<string> delegated_contracts { get; set; }
        //public decimal delegated_balance { get; set; }
        public bool deactivated { get; set; }
        //public int grace_period { get; set; }
        public DateTime Received { get; } = DateTime.UtcNow;
        //public string Hash;
        public decimal? bond;
        public int NumDelegators { get; set; }
        public decimal Bond => bond ?? (balance - frozen_balance + (frozen_balance_by_cycle.Count > 0 ? frozen_balance_by_cycle.Sum(o => o.deposit) : 0));
    }


    public class ContractInfo
    {
        public string manager { get; set; }
        public ulong balance { get; set; }
        //public bool spendable { get; set; }
        public string @delegate { get; set; }
		//public string counter { get; set; }

		//public readonly DateTime Received = DateTime.Now;
		//public string Hash;
    }
    
	public class CryptoComparePrice
    {
        public decimal BTC { get; set; }
        public decimal ETH { get; set; }
        public decimal USD { get; set; }
        public decimal EUR { get; set; }
    }
}
