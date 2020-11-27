namespace TezosNotifyBot.Domain
{
    public class ProposalVote
    {
        public int Id { get; set; }
        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }
        public Proposal Proposal { get; set; }
        public int ProposalID { get; set; }
        public int Level { get; set; }
        public int VotingPeriod { get; set; }
        public int Ballot { get; set; } //0 - поддержка, 1 - yay, 2 - nay, 3 - pass
    }
}