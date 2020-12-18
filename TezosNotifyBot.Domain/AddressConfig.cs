namespace TezosNotifyBot.Domain
{
    public class AddressConfig
    {
        public string Id { get; set; }

        public string Icon { get; set; }

        private AddressConfig()
        {
        }

        public AddressConfig(string address, string icon)
        {
            Id = address;
            Icon = icon;
        }
    }
}