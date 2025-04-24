namespace TezosNotifyBot.Domain
{
    public class AddressConfig
    {
        public string Id { get; set; }

        public string Icon { get; set; }

		public AddressConfig()
		{
            Id = string.Empty;
            Icon = string.Empty;
		}

		public AddressConfig(string address, string icon)
        {
            Id = address;
            Icon = icon;
        }
    }
}