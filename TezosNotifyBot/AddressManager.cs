using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Tezos;
using System.Net;
using Newtonsoft.Json;
using TezosNotifyBot.Tzkt;

namespace TezosNotifyBot
{
	public class AddressManager
	{
		ITzKtClient _tzKt;
		public AddressManager(ITzKtClient tzKt)
		{
			_tzKt = tzKt;
		}

		public ContractInfo GetContract(string addr)
		{
			var contract = _tzKt.GetAccount(addr);
			if (contract == null)
				return new ContractInfo();
			return new ContractInfo {
				balance = contract.balance - contract.frozenDeposit,
				@delegate = contract.Delegate?.Address
			};
		}

		public decimal GetBalance(string addr)
		{
			return GetContract(addr).balance / 1000000M;
		}

		public DelegateInfo GetDelegate(string addr)
		{
			var @delegate = _tzKt.GetAccount(addr);
			if (@delegate == null)
				return null;
			if (@delegate.type != "delegate")
				return null;
			var d = _tzKt.GetDelegators(addr);
			return new DelegateInfo {
				balance = @delegate.balance - @delegate.frozenDeposit,
				deactivated = !@delegate.active,
				staking_balance = @delegate.stakingBalance,
				bond = @delegate.balance,
				delegated_contracts = d.Select(d => d.address).ToList(),
				NumDelegators = @delegate.numDelegators
			};
		}
	}
}