using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
	public class BigmapItem
	{
        public int id { get; set; }
        public bool active { get; set; }
        public string hash { get; set; }
        public string key { get; set; }
        public object value { get; set; }
        public int firstLevel { get; set; }
        public int lastLevel { get; set; }
        public int updates { get; set; }
    }
}
