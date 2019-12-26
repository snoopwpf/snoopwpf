// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.MethodsTab
{
    using System;
    using System.Reflection;

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
