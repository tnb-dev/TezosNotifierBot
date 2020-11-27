namespace TezosNotifyBot.Domain
{
    public class UserAddressDelegation
    {
        public int Id { get; set; }
        public UserAddress UserAddress { get; set; }
        public int UserAddressId { get; set; }
        public Delegate Delegate { get; set; }
        public int DelegateId { get; set; }
    }
}