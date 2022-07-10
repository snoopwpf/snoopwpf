// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System;

public class TypeNamePair : IComparable
{
    public TypeNamePair(Type type)
    {
        this.Type = type ?? throw new ArgumentNullException(nameof(type));
        this.Name = this.Type.Name;
    }

    public BindableType Type { get; }

    public string Name { get; }

    public override string ToString()
    {
        return this.Name;
    }

    #region IComparable Members

    public int CompareTo(object? obj)
    {
        return string.Compare(this.Name, (obj as TypeNamePair)?.Name, StringComparison.Ordinal);
    }

    #endregion
}