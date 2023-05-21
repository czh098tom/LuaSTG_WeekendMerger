using System;
using System.Collections.Generic;
using System.Linq;

namespace WeekendMerger
{
    public static class Extension
    {
        public static IEnumerable<TSource> Shuffle<TSource>(this IEnumerable<TSource> source)
        {
            Random random = new(Guid.NewGuid().GetHashCode());
            var array = source.ToArray();
            for (int i = array.Length - 1; i > 0; i--)
            {   
                int j = random.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return array;
        }
    }
}
