namespace TezosNotifyBot.Domain
{
    public class BalanceUpdate
    {
        public int Id { get; set; }

        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }
        public long Amount { get; set; }

        public int Type { get; set; } // 1 - reward for baking, 2 - reward for endorsing, 3 - missed reward for baking, 4 - missed reward for endorsing
        public int Level { get; set; }
        public int Slots { get; set; }
    }
}