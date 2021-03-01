#nullable enable
namespace TezosNotifyBot.Domain
{
    public class KnownAddress
    {
        public KnownAddress(string address, string name)
        {
            Name = name;
            Address = address;
        }

        public string Name { get; set; }
        public string Address { get; set; }

        public string? PayoutFor { get; set; }
    }
}