// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure.Helpers;

using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

public static class ResourceDictionaryKeyHelpers
{
    public static object GetKeyOfResourceItem(DependencyObject? dependencyObject, object? resourceItem)
    {
        if (dependencyObject is null
            || resourceItem is null)
        {
            return DependencyProperty.UnsetValue;
        }

        return GetKeyOfResourceItemFromElement(dependencyObject, resourceItem)
               ?? GetKeyOfResourceItemFromApplicationResources(resourceItem)
               ?? GetKeyOfResourceItemFromSystemResources(resourceItem)
               ?? DependencyProperty.UnsetValue;
    }

    // Walk up the visual tree, looking for the resourceItem in each frameworkElement's resource dictionary.
    private static object? GetKeyOfResourceItemFromElement(DependencyObject dependencyObject, object resourceItem)
    {
        while (dependencyObject is Visual or Visual3D)
        {
            if (dependencyObject is FrameworkElement fe)
            {
                var resourceKey = GetKeyInResourceDictionary(fe.Resources, resourceItem)
                                  ?? GetKeyInResourceDictionary(FrameworkElementHelper.GetStyle(fe)?.Resources, resourceItem)
                                  ?? GetKeyInResourceDictionary(FrameworkElementHelper.GetTemplate(fe)?.Resources, resourceItem)
                                  ?? GetKeyInResourceDictionary(FrameworkElementHelper.GetThemeStyle(fe)?.Resources, resourceItem);
                if (resourceKey is not null)
                {
                    return resourceKey;
                }
            }
            else if (dependencyObject is FrameworkContentElement fce)
            {
                var resourceKey = GetKeyInResourceDictionary(fce.Resources, resourceItem)
                                  ?? GetKeyInResourceDictionary(FrameworkElementHelper.GetStyle(fce)?.Resources, resourceItem)
                                  ?? GetKeyInResourceDictionary(FrameworkElementHelper.GetThemeStyle(fce)?.Resources, resourceItem);
                if (resourceKey is not null)
                {
                    return resourceKey;
                }
            }

            dependencyObject = VisualTreeHelper.GetParent(dependencyObject) ?? LogicalTreeHelper.GetParent(dependencyObject);
        }

        return null;
    }

    // Check application resources
    private static object? GetKeyOfResourceItemFromApplicationResources(object? resourceItem)
    {
        if (Application.Current is not null)
        {
            var resourceKey = GetKeyInResourceDictionary(Application.Current.Resources, resourceItem);
            if (resourceKey is not null)
            {
                return resourceKey;
            }
        }

        return null;
    }

    // Check system resources
    private static object? GetKeyOfResourceItemFromSystemResources(object? resourceItem)
    {
        foreach (var cacheEntry in SystemResourcesCache.Instance.SystemResources.Reverse())
        {
            {
                var resourceKey = GetKeyInResourceDictionary(cacheEntry.Themed, resourceItem);
                if (resourceKey is not null)
                {
                    return resourceKey;
                }
            }

            // Searching in .Generic causes a lot of exceptions...
            // {
            //     var resourceKey = GetKeyInResourceDictionary(cacheEntry.Generic, resourceItem);
            //     if (resourceKey is not null)
            //     {
            //         return resourceKey;
            //     }
            // }
        }

        return null;
    }

    public static object? GetKeyInResourceDictionary(ResourceDictionary? dictionary, object? resourceItem)
    {
        if (dictionary is null)
        {
            return null;
        }

        foreach (var key in dictionary.Keys)
        {
            if (dictionary.TryGetValue(key, out var item)
                && item == resourceItem)
            {
                return key;
            }
        }

        foreach (var dic in dictionary.MergedDictionaries.Reverse())
        {
            var key = GetKeyInResourceDictionary(dic, resourceItem);
            if (key is not null)
            {
                return key;
            }
        }

        return null;
    }
}