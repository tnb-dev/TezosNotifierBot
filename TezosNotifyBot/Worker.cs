using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
/*
namespace TezosNotifyBot
{
	public class Worker
	{
		Serilog.Core.Logger logger;
		object locker = new object();
		int taskCount = 0;
		WebClientBase wc;

		public event ErrorEventHandler OnError;

		public Worker()
		{
			logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Verbose).WriteTo.File(Path.Combine(TezosBot.LogsPath, "Worker-Full-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10, flushToDiskInterval: new TimeSpan(0, 0, 10)))
				.CreateLogger();
		}

		public void Run(string name, Action<WebClientBase> a)
		{
			Thread t = new Thread(new ThreadStart(() => 
			{
				int number = taskCount++;
				logger.Verbose($"Start thread {name} [{number}]");
				lock(locker)
				{
					try
					{
						logger.Verbose($"Execution begin {name} [{number}]");
						using (wc = new WebClientBase(logger))
							a(wc);
						logger.Verbose($"Execution end {name} [{number}]");
					}
					catch(Exception e)
					{
						logger.Error(e, $"Exection failed: {name} [{number}]");
						OnError?.Invoke($"{name} [{number}]", new ErrorEventArgs(e));
					}
				}
			}));
			t.Start();
		}
	}
}*/
