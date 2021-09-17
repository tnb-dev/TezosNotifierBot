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
                "en" => dateTime.ToString("MMMM d \'at\' hh:mm \'UTC\'", CultureInfo.GetCultureInfo("en")),
                _ => throw new ArgumentOutOfRangeException(nameof(locale), locale, null)
            };
        }
    }
}