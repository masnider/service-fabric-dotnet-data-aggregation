using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMetrics.Common
{
    //Courtesy of https://stackoverflow.com/a/12611341/4852187 
    public static class ConcurrentDictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(
            this ConcurrentDictionary<TKey, Lazy<TValue>> @this,
            TKey key, Func<TKey, TValue> valueFactory
        )
        {
            return @this.GetOrAdd(key,
                (k) => new Lazy<TValue>(() => valueFactory(k))
            ).Value;
        }

        public static TValue AddOrUpdate<TKey, TValue>(
            this ConcurrentDictionary<TKey, Lazy<TValue>> @this,
            TKey key, Func<TKey, TValue> addValueFactory,
            Func<TKey, TValue, TValue> updateValueFactory
        )
        {
            return @this.AddOrUpdate(key,
                (k) => new Lazy<TValue>(() => addValueFactory(k)),
                (k, currentValue) => new Lazy<TValue>(
                    () => updateValueFactory(k, currentValue.Value)
                )
            ).Value;
        }

        public static bool TryGetValue<TKey, TValue>(
            this ConcurrentDictionary<TKey, Lazy<TValue>> @this,
            TKey key, out TValue value
        )
        {
            value = default(TValue);

            var result = @this.TryGetValue(key, out Lazy<TValue> v);

            if (result) value = v.Value;

            return result;
        }

        // this overload may not make sense to use when you want to avoid
        //  the construction of the value when it isn't needed
        public static bool TryAdd<TKey, TValue>(
            this ConcurrentDictionary<TKey, Lazy<TValue>> @this,
            TKey key, TValue value
        )
        {
            return @this.TryAdd(key, new Lazy<TValue>(() => value));
        }

        public static bool TryAdd<TKey, TValue>(
            this ConcurrentDictionary<TKey, Lazy<TValue>> @this,
            TKey key, Func<TKey, TValue> valueFactory
        )
        {
            return @this.TryAdd(key,
                new Lazy<TValue>(() => valueFactory(key))
            );
        }

        public static bool TryRemove<TKey, TValue>(
            this ConcurrentDictionary<TKey, Lazy<TValue>> @this,
            TKey key, out TValue value
        )
        {
            value = default(TValue);

            if (@this.TryRemove(key, out Lazy<TValue> v))
            {
                value = v.Value;
                return true;
            }
            return false;
        }

        public static bool TryUpdate<TKey, TValue>(
            this ConcurrentDictionary<TKey, Lazy<TValue>> @this,
            TKey key, Func<TKey, TValue, TValue> updateValueFactory
        )
        {
            if (!@this.TryGetValue(key, out Lazy<TValue> existingValue))
                return false;

            return @this.TryUpdate(key,
                new Lazy<TValue>(
                    () => updateValueFactory(key, existingValue.Value)
                ),
                existingValue
            );
        }
    }
}
