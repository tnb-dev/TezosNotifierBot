using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NornPool.Model;
using TezosNotifyBot.Domain;

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

        public static string TezToUsd(this decimal tz, Tezos.MarketData md)
        {
            return (tz * md.price_usd).ToString("###,###,###,###,##0.00", CultureInfo.InvariantCulture).Trim();
        }

        public static string TezToBtc(this decimal tz, Tezos.MarketData md)
        {
            return (tz * md.price_btc).ToString("#,###,##0.####", CultureInfo.InvariantCulture).Trim();
        }

        public static MemoryStream CreateZipToMemoryStream(Stream source, string zipEntryName)
        {
            MemoryStream outputMemStream = new MemoryStream();
            ZipOutputStream zipStream = new ZipOutputStream(outputMemStream);

            zipStream.SetLevel(9); //0-9, 9 being the highest level of compression

            ZipEntry newEntry = new ZipEntry(zipEntryName);
            newEntry.DateTime = DateTime.Now;

            zipStream.PutNextEntry(newEntry);

            StreamUtils.Copy(source, zipStream, new byte[4096]);
            zipStream.CloseEntry();

            zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
            zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.

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
    }
}
