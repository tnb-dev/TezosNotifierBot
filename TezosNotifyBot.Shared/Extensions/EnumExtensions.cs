using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TezosNotifyBot.Shared.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var type = value.GetType();
            var member = type.GetMember(value.ToString());

            var displayName = (DisplayAttribute)member[0]
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault();

            return displayName?.Name ?? member[0].Name;
        }
    }
}