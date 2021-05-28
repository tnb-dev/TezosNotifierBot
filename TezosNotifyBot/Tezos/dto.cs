using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TezosNotifyBot.Tezos
{
    public class BlockHeader
    {
        public string protocol { get; set; }
        public string chain_id { get; set; }
        public string hash { get; set; }
        public int level { get; set; }
        public int proto { get; set; }
        public string predecessor { get; set; }
        public DateTime timestamp { get; set; }
        public int validation_pass { get; set; }
        public string operations_hash { get; set; }
        public List<string> fitness { get; set; }
        public string context { get; set; }
        public int priority { get; set; }
        public string proof_of_work_nonce { get; set; }
        public string signature { get; set; }
    }

    public class Operation
    {
        public string protocol { get; set; }
        public string chain_id { get; set; }
        public string hash { get; set; }
        public string branch { get; set; }
        public OperationContent[] contents { get; set; }
        public string signature { get; set; }
    }

    public class OperationContent
    {
        public string kind { get; set; }
        public int level { get; set; }
        public Metadata metadata { get; set; }
        public string source { get; set; }
        public string fee { get; set; }
        public string counter { get; set; }
        public string gas_limit { get; set; }
        public string storage_limit { get; set; }
        public decimal balance { get; set; }
        public string @delegate { get; set; }
        public string amount { get; set; }
        public string destination { get; set; }
		public List<string> proposals { get; set; }
		public int period { get; set; }
		public string proposal { get; set; }
		public string ballot { get; set; }
	}

    public class BlockMetadata
    {
        public string baker { get; set; }
        public Level level { get; set; }
        public List<BalanceUpdate> balance_updates { get; set; }
		public string protocol { get; set; }
		public string next_protocol { get; set; }
		public string voting_period_kind { get; set; }
	}

    public class Metadata
    {
        public List<BalanceUpdate> balance_updates { get; set; }
        public string @delegate { get; set; }
        public List<int> slots { get; set; }
        public OperationResult operation_result { get; set; }
		public List<InternalOperationResult> internal_operation_results { get; set; }
	}

	public class InternalOperationResult
	{
		public string kind { get; set; }
		public string source { get; set; }
		public int nonce { get; set; }
		public string amount { get; set; }
		public string destination { get; set; }
		public OperationResult result { get; set; }
		public string @delegate { get; set; }
	}

	public class OperationResult
    {
        public string status { get; set; }
        public List<string> originated_contracts { get; set; }
		public List<BalanceUpdate> balance_updates { get; set; }
		public long consumed_gas { get; set; }
	}

    public class Level
    {
        public int level { get; set; }
        public int level_position { get; set; }
        public int cycle { get; set; }
        public int cycle_position { get; set; }
        public int voting_period { get; set; }
        public int voting_period_position { get; set; }
        public bool expected_commitment { get; set; }
    }
    public class BalanceUpdate
    {
        public string kind { get; set; }
        public string contract { get; set; }
        public long change { get; set; }
        public string category { get; set; }
        public string @delegate { get; set; }
        public int? level { get; set; }
		public int? cycle { get; set; }
	}

    public class Snapshot
    {
        public int snapshot_cycle { get; set; }
        public int snapshot_index { get; set; }
        public int snapshot_level { get; set; }
        public int snapshot_rolls { get; set; }
    }

    public class RewardsSplit
    {
        public string delegate_staking_balance { get; set; }
        public int delegators_nb { get; set; }
        public List<List<object>> delegators_balance { get; set; }
        public int blocks_rewards { get; set; }
        public int endorsements_rewards { get; set; }
        public int fees { get; set; }
        public int future_blocks_rewards { get; set; }
        public int future_endorsements_rewards { get; set; }
    }

    public class MarketData
    {
        public decimal price_usd { get; set; }
		public decimal price_btc { get; set; }
        public decimal price_eur { get; set; }
    }

    public class FrozenBalanceByCycle
    {
        public int cycle { get; set; }
        public decimal deposit { get; set; }
        public decimal fees { get; set; }
        public decimal rewards { get; set; }
    }

    public class DelegateInfo
    {
        public decimal balance { get; set; }
        public decimal frozen_balance { get; set; }
        public List<FrozenBalanceByCycle> frozen_balance_by_cycle { get; set; }
        public decimal staking_balance { get; set; }
        public List<string> delegated_contracts { get; set; }
        //public decimal delegated_balance { get; set; }
        public bool deactivated { get; set; }
        //public int grace_period { get; set; }
        public DateTime Received { get; } = DateTime.Now;
        public string Hash;
        public decimal? bond;
        public int NumDelegators { get; set; }
        public decimal Bond => bond ?? (balance - frozen_balance + (frozen_balance_by_cycle.Count > 0 ? frozen_balance_by_cycle.Sum(o => o.deposit) : 0));
    }

    public class Delegate
    {
        public bool setable { get; set; }
        public string value { get; set; }
    }

    public class ContractInfo
    {
        public string manager { get; set; }
        public long balance { get; set; }
        //public bool spendable { get; set; }
        public string @delegate { get; set; }
		//public string counter { get; set; }

		//public readonly DateTime Received = DateTime.Now;
		public string Hash;
    }

	public class ContractInfo_alfa
	{
		public string manager { get; set; }
		public long balance { get; set; }
		public bool spendable { get; set; }
		public Delegate @delegate { get; set; }
		public string counter { get; set; }
	}

	public class CryptoComparePrice
    {
        public decimal BTC { get; set; }
        public decimal USD { get; set; }
        public decimal EUR { get; set; }
    }

    public class BakingRights
    {
        public int level { get; set; }
        public string @delegate { get; set; }
        public int priority { get; set; }
        public DateTime estimated_time { get; set; }
    }

    public class EndorsingRights
    {
        public int level { get; set; }
        public string @delegate { get; set; }
        public List<int> slots { get; set; }
        public DateTime estimated_time { get; set; }
    }

    public class TzScanDelegates
    {
        public string kind { get; set; }
        public string address { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string logo { get; set; }
        public object descr { get; set; }
        public DateTime sponsored { get; set; }
        public string logo2 { get; set; }
    }

	public class VoteListing
	{
		public string pkh { get; set; }
		public int rolls { get; set; }
	}

	public class Ballots
	{
		public int yay { get; set; }
		public int nay { get; set; }
		public int pass { get; set; }
	}

	public class Ballot
	{
		public string pkh { get; set; }
		public string ballot { get; set; }
	}

	public class Constants
	{
		public int proof_of_work_nonce_size { get; set; }
		public int nonce_length { get; set; }
		public int max_revelations_per_block { get; set; }
		public int max_operation_data_length { get; set; }
		public int max_proposals_per_delegate { get; set; }
		public int preserved_cycles { get; set; }
		public int blocks_per_cycle { get; set; }
		public int blocks_per_commitment { get; set; }
		public int blocks_per_roll_snapshot { get; set; }
		public int blocks_per_voting_period { get; set; }
		public List<string> time_between_blocks { get; set; }
		public int endorsers_per_block { get; set; }
		public string hard_gas_limit_per_operation { get; set; }
		public string hard_gas_limit_per_block { get; set; }
		public string proof_of_work_threshold { get; set; }
		public decimal tokens_per_roll { get; set; }
		public int michelson_maximum_type_size { get; set; }
		public string seed_nonce_revelation_tip { get; set; }
		public int origination_size { get; set; }
		public long block_security_deposit { get; set; }
		public long endorsement_security_deposit { get; set; }
		//public long block_reward { get; set; }
		//public long endorsement_reward { get; set; }
		public long cost_per_byte { get; set; }
		public long hard_storage_limit_per_operation { get; set; }
		public string test_chain_duration { get; set; }
	}
}
