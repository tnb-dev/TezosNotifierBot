using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.BetterCallDev
{
    public class Result
    {
        public ulong consumed_gas { get; set; }
        public ulong storage_size { get; set; }
    }

    public class Parameters
    {
        public string prim { get; set; }
        public string type { get; set; }
        public IList<Child> children { get; set; }
    }

    public class Child
    {
        public string prim { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string from { get; set; }
        public string diff_type { get; set; }
        public string value { get; set; }
        public IList<Child> children { get; set; }
    }

    public class StorageDiff
    {
        public string prim { get; set; }
        public string type { get; set; }
        public IList<Child> children { get; set; }
    }

    public class Operation
    {
        public int level { get; set; }
        public ulong fee { get; set; }
        public int counter { get; set; }
        public ulong gas_limit { get; set; }
        public ulong storage_limit { get; set; }
        public ulong amount { get; set; }
        public int content_index { get; set; }
        public Result result { get; set; }
        public Parameters parameters { get; set; }
        public StorageDiff storage_diff { get; set; }
        public DateTime timestamp { get; set; }
        public string id { get; set; }
        public string protocol { get; set; }
        public string hash { get; set; }
        public string network { get; set; }
        public string kind { get; set; }
        public string source { get; set; }
        public string destination { get; set; }
        public string status { get; set; }
        public string entrypoint { get; set; }
        public bool @internal { get; set; }
        public bool mempool { get; set; }
    }
}
