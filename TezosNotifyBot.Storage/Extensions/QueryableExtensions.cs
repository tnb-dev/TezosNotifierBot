using System;
using System.Linq;
using System.Linq.Expressions;

namespace TezosNotifyBot.Storage.Extensions
{
    public static class QueryableExtensions
    {

        /// <summary>
        /// Apply predicate if it defined
        /// </summary>
        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate)
        {
            return predicate != null 
                ? queryable.Where(predicate) 
                : queryable;
        }
        
    }
}