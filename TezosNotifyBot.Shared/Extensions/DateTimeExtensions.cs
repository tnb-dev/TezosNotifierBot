using System;
using System.Globalization;

namespace TezosNotifyBot.Shared.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToLocaleString(this DateTime dateTime) => dateTime.ToString("MMMM d \'at\' hh:mm \'UTC\'", CultureInfo.GetCultureInfo("en"));
    }
}