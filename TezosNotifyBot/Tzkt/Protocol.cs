using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Tzkt
{
    public class Protocol
    {
        public int code { get; set; }
        public string hash { get; set; }
        public int firstLevel { get; set; }
        public int firstCycle { get; set; }
        public int firstCycleLevel { get; set; }
        public Constants constants { get; set; }
    }
}
