namespace TezosNotifyBot.Storage.Extensions
{
    public static class StringExtensions
    {
        public static string Escape(this string str) => str.Replace("'", "''");
    }
}