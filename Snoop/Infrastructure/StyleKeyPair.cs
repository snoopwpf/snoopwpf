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
