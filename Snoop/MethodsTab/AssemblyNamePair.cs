using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Snoop.MethodsTab
{
    public class AssemblyNamePair : IComparable
    {
        public string Name { get; set; }

        public Assembly Assembly { get; set; }

        public override string ToString()
        {
            return Name;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return Name.CompareTo(((AssemblyNamePair)obj).Name);
        }

        #endregion
    }
}
