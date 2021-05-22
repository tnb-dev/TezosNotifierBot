using System;

namespace TezosNotifyBot.Tzkt
{
    // TODO: Add inheritance by Type field
    // https://api.tzkt.io/#operation/Accounts_GetOperations
    // https://github.com/JamesNK/Newtonsoft.Json/issues/1331#issuecomment-495813721
    // https://gist.github.com/StevenLiekens/82ddcf1823ee91cf6d5edfcdb1f6a591
    public class Operation
    {
        public int Id { get; set; }
        
        public string Type { get; set; }
        
        public int Level { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public string Block { get; set; }
        
        public string Hash { get; set; }

        public Baker @delegate { get; set; }
        public int slots { get; set; }
        public int deposit { get; set; }
        public int rewards { get; set; }
}
    
	public class OperationPenalty: Operation
    {
        public Baker baker { get; set; }
        public int missedLevel { get; set; }
        public ulong lostReward { get; set; }
        public ulong lostFees { get; set; }
        public decimal TotalLost => (lostReward + lostFees) / 1000000M;
    }
}
