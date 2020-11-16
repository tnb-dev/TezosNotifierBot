using System;
using System.Text;

namespace TezosNotifyBot
{
    class Program
    {
        static void Main(string[] args)
        {
			Console.WriteLine("Бот запущен " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var npb = new TezosBot();
            npb.Run();
        }
    }
}
