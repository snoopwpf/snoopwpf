// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System;
using System.Reflection;

public class AssemblyNamePair : IComparable
{
    public AssemblyNamePair(Assembly assembly)
    {
        this.Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        this.Name = this.Assembly.FullName!;
    }

    public Assembly Assembly { get; }

    public string Name { get; }

    public override string ToString()
    {
        return this.Name;
    }

    #region IComparable Members

    public int CompareTo(object? obj)
    {
        return string.Compare(this.Name, (obj as AssemblyNamePair)?.Name, StringComparison.Ordinal);
    }

    #endregion
}