using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSee.Helpers
{
    internal static class Extensions
    {
        public static string GetNumbers(this string text)
        {
            text = text ?? string.Empty;
            return new string(text.Where(p => char.IsDigit(p)).ToArray());
        }

        private static Random random = new Random();

        public static T GetRandomElement<T>(this IEnumerable<T> list)
        {
            if (list.Count() == 0)
                return default(T);
            try
            {
                return list.ElementAt(random.Next(list.Count()));
            }
            catch(Exception ex)
            {
                return list.FirstOrDefault();
            }
        }
    }
}
