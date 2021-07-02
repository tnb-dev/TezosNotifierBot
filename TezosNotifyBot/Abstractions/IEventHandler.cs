using System.Threading.Tasks;

namespace TezosNotifyBot.Abstractions
{
    public interface IEventHandler<in T> where T: class
    {
        Task Process(T subject);
    }
}