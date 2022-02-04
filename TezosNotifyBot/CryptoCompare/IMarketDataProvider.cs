using System;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Tezos;

namespace TezosNotifyBot.CryptoCompare
{
	public interface IMarketDataProvider
	{
		MarketData GetMarketData();
	}
}
