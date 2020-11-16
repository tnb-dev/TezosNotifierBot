using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace TezosNotifyBot
{
	public class WebClientBase : IDisposable
	{
		System.Net.WebClient wc = new System.Net.WebClient();
		Serilog.Core.Logger logger;

		public Serilog.Core.Logger Logger => logger;

		public WebClientBase(Serilog.Core.Logger logger)
		{
			this.logger = logger;
		}

		public void Dispose()
		{
			if (wc != null)
				wc.Dispose();
		}

		public string Download(string addr)
		{
			try
			{
				logger?.Verbose("download " + addr);
				lock (wc)
				{
					var result = wc.DownloadString(addr);
					logger?.Verbose("download complete: " + addr);
					return result;
				}
			}
			catch (WebException we)
			{
				logger?.Error(we, "Error downloading from " + addr);
				var rs = we.Response?.GetResponseStream();
				if (rs != null)
				{
					var err = new StreamReader(rs).ReadToEnd();
					logger?.Error(we, "Error downloading from " + addr + ". Response: " + err);

				}
				throw;
			}
			catch (Exception e)
			{
				logger?.Error(e, "Error downloading from " + addr);
				throw;
			}
		}
	}
}
