// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure.Helpers
{
    using System.Windows;
    using System.Windows.Media;

    public static class ResourceDictionaryKeyHelpers
    {
        public static string GetKeyOfResourceItem(DependencyObject? dependencyObject, object? resourceItem)
        {
            if (dependencyObject is null
                || resourceItem is null)
            {
                return string.Empty;
            }

            // Walk up the visual tree, looking for the resourceItem in each frameworkElement's resource dictionary.
            while (dependencyObject is not null)
            {
                if (dependencyObject is FrameworkElement frameworkElement)
                {
                    var resourceKey = GetKeyInResourceDictionary(frameworkElement.Resources, resourceItem);
                    if (resourceKey is not null)
                    {
                        return resourceKey;
                    }
                }
                else
                {
                    break;
                }

                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            // check the application resources
            if (Application.Current is not null)
            {
                var resourceKey = GetKeyInResourceDictionary(Application.Current.Resources, resourceItem);
                if (resourceKey is not null)
                {
                    return resourceKey;
                }
            }

            return string.Empty;
        }

        public static string? GetKeyInResourceDictionary(ResourceDictionary dictionary, object? resourceItem)
        {
            foreach (var key in dictionary.Keys)
            {
                if (dictionary.TryGetValue(key, out var item)
                    && item == resourceItem)
                {
                    return key?.ToString();
                }
            }

            if (dictionary.MergedDictionaries is not null)
            {
                foreach (var dic in dictionary.MergedDictionaries)
                {
                    var name = GetKeyInResourceDictionary(dic, resourceItem);
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }

            return null;
        }
    }
}