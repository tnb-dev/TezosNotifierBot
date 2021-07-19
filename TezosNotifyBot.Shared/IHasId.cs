namespace TezosNotifyBot.Shared
{
    public interface IHasId<T>
    {
        public T Id { get; set; }
    }
}