namespace TezosNotifyBot.Domain
{
    public class DelegateRewards
    {
        public int Id { get; set; }

        public long Rewards { get; set; }
        public long Accured { get; set; }
        public long Delivered { get; set; }

        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }

        public int Cycle { get; set; }
    }
}