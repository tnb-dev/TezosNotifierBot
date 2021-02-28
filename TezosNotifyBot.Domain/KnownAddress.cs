#nullable enable
namespace TezosNotifyBot.Domain
{
    public class KnownAddress
    {
        public KnownAddress(string name, string address)
        {
            Name = name;
            Address = address;
        }

        public string Name { get; set; }
        public string Address { get; set; }

        public string? PayoutFor { get; set; }
    }
}