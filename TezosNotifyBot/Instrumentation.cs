using System;
using System.Diagnostics;

namespace TezosNotifyBot
{
	public class Instrumentation : IDisposable
	{
		internal const string ActivitySourceName = "TNB";

		public Instrumentation()
		{
			ActivitySource = new ActivitySource(ActivitySourceName);
		}

		public ActivitySource ActivitySource { get; }

		public void Dispose()
		{
			ActivitySource.Dispose();
		}
	}
}