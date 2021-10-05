using System;
using System.Collections.Generic;
using System.Linq;

namespace TezosNotifyBot.Shared.Models
{
    public class Paginated<T>
    {
        public int Page { get; }
        public int Take { get; }
        public int Pages { get; }
        public int Total { get; }
        public T[] Items { get; }

        public Paginated(int page, int take, int total, T[] items)
        {
            Page = page;
            Take = take;
            Total = total;
            Items = items;
            Pages = (int)Math.Ceiling((decimal)total / take);
        }
    }

    public static class PaginatedExtensions
    {
        public static Paginated<T> Paginate<T>(this IEnumerable<T> enumerable, int page, int take)
        {
            return new Paginated<T>(
                page,
                take,
                enumerable.Count(),
                enumerable.Skip((page - 1) * take).Take(take).ToArray()
            );
        }
    }
}