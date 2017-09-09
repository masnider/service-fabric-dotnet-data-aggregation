using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BladeRuiner.Common.ServiceUtilities
{
    public static class StringUtils
    {
        private static Random random = new Random((int)DateTime.Now.Ticks);

        private static IEnumerable<string> GraphemeClusters(string s)
        {
            var enumerator = StringInfo.GetTextElementEnumerator(s);
            while (enumerator.MoveNext())
            {
                yield return (string)enumerator.Current;
            }
        }

        public static string ReverseString(string s)
        {
            return string.Join("", GraphemeClusters(s).Reverse().ToArray());
        }

        public static string ReverseString(char[] chars)
        {
            return ReverseString(new string(chars));
        }

        public static string RandomString(int size = 10)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                lock (random)
                {
                    //ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                    ch = (char)random.Next('a', 'z' + 1);
                }
                builder.Append(ch);
            }

            return builder.ToString();
        }

    }
}
