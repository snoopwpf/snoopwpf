using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snoop.MethodsTab
{
    public class TypeNamePair : IComparable
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public override string ToString()
        {
            return Name;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return Name.CompareTo(((TypeNamePair)obj).Name);
        }

        #endregion
    }
}
