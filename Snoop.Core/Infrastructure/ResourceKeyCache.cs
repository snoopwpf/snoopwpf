// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System.Collections.Generic;

    public static class ResourceKeyCache
    {
        private static readonly Dictionary<object, string> keys = new();

        public static string? GetKey(object element)
        {
            if (keys.TryGetValue(element, out var key))
            {
                return key;
            }

            return null;
        }

        public static void Cache(object element, string key)
        {
            if (keys.ContainsKey(element) == false)
            {
                keys.Add(element, key);
            }
        }

        public static bool Contains(object element)
        {
            return keys.ContainsKey(element);
        }
    }
}