using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snoop.MethodsTab
{
    public class TypeComparerByName : IComparer<Type>
    {
        #region IComparer<Type> Members

        public int Compare(Type x, Type y)
        {
            return x.Name.CompareTo(y.Name);
        }

        #endregion
    }
}
