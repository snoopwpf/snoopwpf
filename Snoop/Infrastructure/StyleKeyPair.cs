// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;

namespace Snoop.Infrastructure
{
    public class StyleKeyPair : ISkipDelve
    {
        public Style Style { get; set; }

        [DisplayName("x:Key")]
        public string Key { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Key) ? Style.ToString() : Key + " (Style)";
        }

        #region ISkipDelve Members

        public object NextValue
        {
            get
            {
                return Style;
            }
        }

        public Type NextValueType
        {
            get
            {
                return typeof(Style);
            }
        }

        #endregion
    }
}
