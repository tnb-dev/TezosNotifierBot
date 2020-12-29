using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public interface ITzKtClient
	{
		Head GetHead();
	}
}
