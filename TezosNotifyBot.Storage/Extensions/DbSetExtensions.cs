using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TezosNotifyBot.Shared;

namespace TezosNotifyBot.Storage.Extensions
{
    public static class DbSetExtensions
    {
        public static Task<T> ByIdAsync<T, TKey>(this DbSet<T> dbSet, TKey key)
            where T : class, IHasId<TKey>
        {
            return dbSet.SingleOrDefaultAsync(x => x.Id.Equals(key));
        }
    }
}