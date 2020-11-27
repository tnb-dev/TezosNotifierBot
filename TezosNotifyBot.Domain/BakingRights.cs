namespace TezosNotifyBot.Domain
{
    public class BakingRights
    {
        public int Id { get; set; }

        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }
        
        public int Level { get; set; }
    }
}