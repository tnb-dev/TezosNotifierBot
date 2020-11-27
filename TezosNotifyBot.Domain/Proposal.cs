using System.Collections.Generic;

namespace TezosNotifyBot.Domain
{
    public class Proposal
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public string Name { get; set; }
        public int Period { get; set; }
        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }

        public string HashTag()
        {
            return " #" + (Hash.Substring(0, 7) + Hash.Substring(Hash.Length - 5)).ToLower();
        }
        public int VotedRolls;
        public List<UserAddress> Delegates;
    }
}