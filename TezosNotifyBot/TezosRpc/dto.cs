using System;
using System.Collections.Generic;
using System.Text;

namespace TezosNotifyBot.TezosRpc
{
	public class Block
	{
		public string protocol { get; set; }
		public string chain_id { get; set; }
		public string hash { get; set; }
		public Header header { get; set; }
		public BlockMetadata metadata { get; set; }
		public List<List<Operation>> operations { get; set; }
	}

	public class Header
	{
		public int level { get; set; }
		public byte proto { get; set; }
		public string predecessor { get; set; }
		public DateTime timestamp { get; set; }
		public byte validation_pass { get; set; }
		public string operations_hash { get; set; }
		public List<string> fitness { get; set; }
		public string context { get; set; }
		public ushort priority { get; set; }
		public string proof_of_work_nonce { get; set; }
		public string seed_nonce_hash { get; set; }
		public string signature { get; set; }
	}

	public class BlockMetadata
	{
		public string protocol { get; set; }
		public string next_protocol { get; set; }
		public TestChainStatus test_chain_status { get; set; }
		public int max_operations_ttl { get; set; }
		public int max_operation_data_length { get; set; }
		public int max_block_header_length { get; set; }
		public List<MaxOperationListLength> max_operation_list_length { get; set; }
		public string baker { get; set; }
		public Level level { get; set; }
		public string voting_period_kind { get; set; }
		public object nonce_hash { get; set; }
		public string consumed_gas { get; set; }
		public List<string> deactivated { get; set; }
		public List<BalanceUpdate> balance_updates { get; set; }
	}

	public class TestChainStatus
	{
		public string status { get; set; }
		public string chain_id { get; set; }
		public string genesis { get; set; }
		public string protocol { get; set; }
		public DateTime? expiration { get; set; }
	}

	public class MaxOperationListLength
	{
		public int max_size { get; set; }
		public int max_op { get; set; }
	}

	public class Level
	{
		public int level { get; set; }
		public int level_position { get; set; }
		public int cycle { get; set; }
		public int cycle_position { get; set; }
		public int voting_period { get; set; }
		public int voting_period_position { get; set; }
		public bool expected_commitment { get; set; } //Tells wether the baker of this block has to commit a seed nonce hash
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

	public class Operation
	{
		public string protocol { get; set; }
		public string chain_id { get; set; }
		public string hash { get; set; }
		public string branch { get; set; }
		public List<Content> contents { get; set; }
		public string signature { get; set; }
	}

	public class Content
	{
		public string kind { get; set; }
		public int? level { get; set; }
		public string nonce { get; set; }
		//op1
		//op2
		//bh1
		//bh2
		public string pkh { get; set; }
		public string secret { get; set; }
		public string source { get; set; }
		public int? period { get; set; }
		public List<string> proposals { get; set; }
		public string proposal { get; set; }
		public string ballot { get; set; } //"nay" | "yay" | "pass"
		public ulong fee { get; set; }
		public ulong counter { get; set; }
		public ulong gas_limit { get; set; }
		public ulong storage_limit { get; set; }
		public string public_key { get; set; }
		public ulong amount { get; set; }
		public string destination { get; set; }
		//parameters
		public ulong balance { get; set; }
		public string @delegate { get; set; }
		//script
		//public Metadata metadata { get; set; }
	}
}
