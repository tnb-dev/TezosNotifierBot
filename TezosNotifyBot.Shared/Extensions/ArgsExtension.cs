namespace TezosNotifyBot.Shared.Extensions
{
    public static class ArgsExtensions
    {
        public static int GetInt(this string[] args, int index) 
            => int.TryParse(args[index], out var result) ? result : int.MinValue;
    }
}