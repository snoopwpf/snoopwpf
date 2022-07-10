// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

// ReSharper disable once CheckNamespace
namespace Snoop;

using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class ObservableCollectionExtensions
{
    public static void UpdateWith<T>(this ObservableCollection<T> target, IReadOnlyCollection<T> source)
    {
        if (target.Count > 0)
        {
            target.Clear();
        }

        if (source is null
            || source.Count <= 0)
        {
            return;
        }

        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}