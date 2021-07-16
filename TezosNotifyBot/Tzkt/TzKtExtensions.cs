namespace TezosNotifyBot.Tzkt
{
    public static class TzKtExtensions
    {

        public static Account GetAccount(this ITzKtClient client, string address)
        {
            return client.Download<Account>($"v1/accounts/{address}");
        }
        public static Account GetDelegate(this ITzKtClient client, string address)
        {
            return client.Download<Account>($"v1/delegates/{address}");
        }
    }
}