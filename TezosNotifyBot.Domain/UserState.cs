namespace TezosNotifyBot.Domain
{
    public enum UserState
    {
        Default = 0,
        Support = 1,
        Broadcast = 2,
        SetAmountThreshold = 3,
        SetDlgAmountThreshold = 4,
        SetName = 5,
        NotifyFollowers = 6
    }
}