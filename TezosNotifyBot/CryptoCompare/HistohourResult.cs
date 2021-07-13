using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.CryptoCompare
{
	public class HistohourResult
	{
        public string Response { get; set; }
        public string Message { get; set; }
        public bool HasWarning { get; set; }
        public int Type { get; set; }
        public ResultData Data { get; set; }
    }
    public class ResultData
    {
        public bool Aggregated { get; set; }
        public int TimeFrom { get; set; }
        public int TimeTo { get; set; }
        public List<Datum> Data { get; set; }
    }

    public class Datum
    {
        public DateTime Timestamp => DateTimeOffset.FromUnixTimeSeconds(time).DateTime;
        public int time { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double open { get; set; }
        public double volumefrom { get; set; }
        public double volumeto { get; set; }
        public double close { get; set; }
        public string conversionType { get; set; }
        public string conversionSymbol { get; set; }
    }
}
