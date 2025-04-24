using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TezosNotifyBot.Model;
using TezosNotifyBot.Domain;
using TezosNotifyBot.Shared.Extensions;

namespace TezosNotifyBot
{
    public static class Utils
    {
        public static string TezToString(this decimal tz)
        {
            if (Math.Abs(tz) > 0.001M)
                return tz.ToString("###,###,###,###,##0.###", CultureInfo.InvariantCulture).Trim() + " XTZ";
            else
                return (tz * 1000).ToString("##0.###", CultureInfo.InvariantCulture).Trim() + " mXTZ";
        }

        public static string AmountToString(this decimal amount, Token token)
        {
            if (token == null)
                return amount.TezToString();
            return amount.ToString("###,###,###,###,##0.########", CultureInfo.InvariantCulture).Trim() + " " +
                   token.Symbol;
        }

        public static string TezToUsd(this decimal tz, Tezos.MarketData md)
            => (tz * md.price_usd).ToString("###,###,###,###,##0.00", CultureInfo.InvariantCulture).Trim();

        public static string TezToBtc(this decimal tz, Tezos.MarketData md)
            => (tz * md.price_btc).ToString("#,###,##0.####", CultureInfo.InvariantCulture).Trim();

        public static string TezToEur(this decimal tz, Tezos.MarketData md)
            => (tz * md.price_eur).ToString("#,###,##0.####", CultureInfo.InvariantCulture).Trim();

        public static string TezToCurrency(this decimal tz, Tezos.MarketData md, UserCurrency currency)
        {
            var code = currency.GetDisplayName();
            return (tz * md.CurrencyRate((Currency) currency))
                .ToString($"###,###,###,###,##0.00 {code}", CultureInfo.InvariantCulture).Trim();
        }
        
        public static string TezToCurrency(this decimal tz, Tezos.MarketData md, User user) 
            => tz.TezToCurrency(md, user.Currency);

        public static MemoryStream CreateZipToMemoryStream(Stream source, string zipEntryName)
        {
            MemoryStream outputMemStream = new MemoryStream();
            ZipOutputStream zipStream = new ZipOutputStream(outputMemStream);

            zipStream.SetLevel(9); //0-9, 9 being the highest level of compression

            ZipEntry newEntry = new ZipEntry(zipEntryName);
            newEntry.DateTime = DateTime.UtcNow;

            zipStream.PutNextEntry(newEntry);

            StreamUtils.Copy(source, zipStream, new byte[4096]);
            zipStream.CloseEntry();

            zipStream.IsStreamOwner = false; // False stops the Close also Closing the underlying stream.
            zipStream.Close(); // Must finish the ZipOutputStream before using outputMemStream.

            outputMemStream.Position = 0;
            return outputMemStream;
        }

        /// <summary>
        /// Temporary added as extension method 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsAdmin(this User user, TelegramOptions config)
            => config.DevUsers.Contains(user.Username);

        public static decimal TokenAmountToDecimal(string str, int decimals)
        {
            string s1 = str.Length > decimals ? str.Substring(0, str.Length - decimals) : "0";
            string s2 = str.Length > decimals ? str.Substring(str.Length - decimals) : str.PadLeft(decimals, '0');
            return decimal.Parse(s1 + "." + s2, CultureInfo.InvariantCulture);
        }
    }
}