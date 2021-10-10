using System;
using System.Collections.Generic;
using System.Linq;

namespace TezosNotifyBot.Shared.Models
{
    public class Paginated<T>
    {
        public int Page { get; }
        public int Take { get; }
        public bool HasPrev { get; set; }
        public bool HasNext { get; set; }
        public T[] Items { get; }

        public Paginated(int page, int take, T[] items, bool hasPrev, bool hasNext)
        {
            Page = page;
            Take = take;
            Items = items;
            HasPrev = hasPrev;
            HasNext = hasNext;
        }
    }

    public static class PaginatedExtensions
    {
        public static Paginated<T> ToFlexPagination<T>(this IEnumerable<T> enumerable, int page, int take)
        {
            var items = enumerable.ToArray();
            var length = items.Length;
            
            return new Paginated<T>(
                page,
                take,
                items.Take(take).ToArray(),
                page != 1,
                length > take
            );
        }
    }
}