using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.Model
{
    public class Command
    {
        public string username { get; set; }
        public string commandname { get; set; }
        public string filepath { get; set; }
        public string arguments { get; set; }
    }
}
