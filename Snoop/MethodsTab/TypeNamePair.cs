// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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
