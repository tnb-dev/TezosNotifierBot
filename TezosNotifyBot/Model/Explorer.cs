using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TezosNotifyBot.Model
{
	public class t
	{
		public static string block(int blocknumber) => string.Format("https://tzkt.io/{0}?utm_source=tezosnotifierbot", blocknumber);
		public static string account(string addr) => string.Format("https://tzkt.io/{0}?utm_source=tezosnotifierbot", addr);
		public static string op(string ophash) => string.Format("https://tzkt.io/{0}?utm_source=tezosnotifierbot", ophash);
		public static string url_vote(int period) => string.Format("https://www.tezosagora.org/period/{0}?utm_source=tezosnotifierbot", period);
	}

	public class t1
	{
		public string block(int blocknumber) => string.Format("https://tzkt.io/{0}?utm_source=tezosnotifierbot", blocknumber);
		public string account(string addr) => string.Format("https://tzkt.io/{0}?utm_source=tezosnotifierbot", addr);
		public string op(string ophash) => string.Format("https://tzkt.io/{0}?utm_source=tezosnotifierbot", ophash);
		public string url_vote(int period) => string.Format("https://www.tezosagora.org/period/{0}?utm_source=tezosnotifierbot", period);
	}
}