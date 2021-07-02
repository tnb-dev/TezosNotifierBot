using System.Threading.Tasks;

namespace TezosNotifyBot.Abstractions
{
    public interface IEventDispatcher
    {
        Task Dispatch<T>(T subject) where T : class;
    }
}