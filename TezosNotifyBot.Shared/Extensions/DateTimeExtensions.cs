using System;
using System.Globalization;

namespace TezosNotifyBot.Shared.Extensions
{
    public static class DateTimeExtensions
    {

        public static string ToLocaleString(this DateTime dateTime, string locale)
        {
            return locale switch
            {
                "ru" => dateTime.ToString("d MMMM \'в\' hh:mm \'UTC\'", CultureInfo.GetCultureInfo("ru")),
                _ => dateTime.ToString("MMMM d \'at\' hh:mm \'UTC\'", CultureInfo.GetCultureInfo("en"))
            };
        }
    }
}