// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab
{
    using System;

    public class TypeNamePair : IComparable
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public override string ToString()
        {
            return this.Name;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return this.Name.CompareTo(((TypeNamePair)obj).Name);
        }

        #endregion
    }
}
