using TezosNotifyBot.Storage;

namespace TezosNotifyBot.Commands
{
    public abstract class BaseHandler
    {
        protected TezosDataContext Db { get; }
        protected TezosBotFacade Bot { get; }

        public BaseHandler(TezosDataContext db, TezosBotFacade botClient)
        {
            Db = db;
            Bot = botClient;
        }

    }
}