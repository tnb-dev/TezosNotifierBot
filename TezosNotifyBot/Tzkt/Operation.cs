using System;
using System.Collections.Generic;

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
    }
    
	public class OperationPenalty: Operation
    {
        public Baker baker { get; set; }
        public int missedLevel { get; set; }
        public ulong lostReward { get; set; }
        public ulong lostFees { get; set; }
        public decimal TotalLost => (lostReward + lostFees) / 1000000M;
    }
    
    public class Endorsement : Operation
    {
        public Baker @delegate { get; set; }
        public int Slots { get; set; }
        public long Deposit { get; set; }
        public long Rewards { get; set; }
    }

    public class Transaction : Operation
    {
        public ulong Counter { get; set; }
        public Address Sender { get; set; }
        public ulong GasLimit { get; set; }
        public ulong GasUsed { get; set; }
        public ulong StorageLimit { get; set; }
        public ulong StorageUsed { get; set; }
        public ulong BakerFee { get; set; }
        public ulong StorageFee { get; set; }
        public ulong AllocationFee { get; set; }
        public Address Target { get; set; }
        public ulong Amount { get; set; }
        public string Status { get; set; }
        public bool HasInternals { get; set; }
        public Parameter Parameter { get; set; }
        public string Parameters { get; set; }
        public Address Initiator { get; set; }
        public ulong? nonce { get; set; }
        public List<Error> errors { get; set; }
    }

    public class Origination : Operation
    {
        public ulong Counter { get; set; }
        public Address Initiator { get; set; }
        public Address Sender { get; set; }
        public ulong Nonce { get; set; }
        public ulong GasLimit { get; set; }
        public ulong GasUsed { get; set; }
        public ulong StorageLimit { get; set; }
        public ulong StorageUsed { get; set; }
        public ulong BakerFee { get; set; }
        public ulong StorageFee { get; set; }
        public ulong AllocationFee { get; set; }        
        public ulong ContractBalance { get; set; }
        public Address ContractManager { get; set; }
        public Address ContractDelegate { get; set; }
        public string Status { get; set; }
        public OriginatedContract OriginatedContract { get; set; }
    }

    public class Delegation : Operation
    {
        public ulong Counter { get; set; }
        public Address Sender { get; set; }
        public ulong GasLimit { get; set; }
        public ulong GasUsed { get; set; }
        public ulong BakerFee { get; set; }
        public ulong Amount { get; set; }
        public Baker NewDelegate { get; set; }
        public string Status { get; set; }
        public Baker PrevDelegate { get; set; }
    }
}
