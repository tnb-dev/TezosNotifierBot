namespace TezosNotifyBot.Domain
{
    public class EndorsingRights
    {
        public int Id { get; set; }

        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }

        public int Level { get; set; }
        public int SlotCount { get; set; }
    }
}