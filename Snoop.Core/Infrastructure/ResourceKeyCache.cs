// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System.Collections.Generic;
using System.Windows;
using Snoop.Infrastructure.Helpers;

public class ResourceKeyCache : ICacheManaged
{
    private readonly Dictionary<object, object> keys = new();

    public static readonly ResourceKeyCache Instance = new();

    private ResourceKeyCache()
    {
    }

    public object? GetOrAddKey(DependencyObject element, object value)
    {
        var resourceKey = this.GetKey(value);

        if (resourceKey is null)
        {
            resourceKey = ResourceDictionaryKeyHelpers.GetKeyOfResourceItem(element, value);
            this.Cache(value, resourceKey);
        }

        return resourceKey;
    }

    public object? GetKey(object value)
    {
        if (this.keys.TryGetValue(value, out var key))
        {
            return key;
        }

        return null;
    }

    public void Cache(object value, object key)
    {
        if (this.keys.ContainsKey(value) == false)
        {
            this.keys.Add(value, key);
        }
    }

    public bool Contains(object element)
    {
        return this.keys.ContainsKey(element);
    }

    public void Activate()
    {
    }

    public void Dispose()
    {
        this.keys.Clear();
    }
}