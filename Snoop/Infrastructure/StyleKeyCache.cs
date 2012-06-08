// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Snoop.Infrastructure
{
    public static class StyleKeyCache
    {
        private static Dictionary<Style, string> Keys = new Dictionary<Style, string>();

        public static string GetKey(Style style)
        {
            string key;
            if (Keys.TryGetValue(style, out key))
                return key;

            return null;
        }

        public static void CacheStyle(Style style, string key)
        {
            if (!Keys.ContainsKey(style))
            {
                Keys.Add(style, key);
            }
        }

        public static bool ContainsStyle(Style style)
        {
            return Keys.ContainsKey(style);
        }
    }
}
