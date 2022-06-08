using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeekendMerger
{
    public static class Extension
    {
        public class RandomComparer<TKey> : IComparer<TKey>
        {
            readonly Random random = new(DateTime.Now.Millisecond);

            public int Compare(TKey? x, TKey? y) => random.Next(3) - 1;
        }

        public static IEnumerable<TSource> Shuffle<TSource>(this IEnumerable<TSource> source)
        {
            return source.OrderBy(x => x, new RandomComparer<TSource>());
        }
    }
}
