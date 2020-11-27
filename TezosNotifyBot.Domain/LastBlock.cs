namespace TezosNotifyBot.Domain
{
    public class LastBlock
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Priority { get; set; }
        public string Hash { get; set; }
    }
}