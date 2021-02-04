using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.BetterCallDev
{
	public interface IBetterCallDevClient
	{
		Account GetAccount(string address);
		IEnumerable<Token> GetTokens(int minLevel);
		List<Operation> GetOperations(string ophash);
	}
}
