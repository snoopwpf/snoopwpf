// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Snoop.Infrastructure
{
    public static class ResourceDictionaryKeyHelpers
    {
        public static string GetKeyOfStyle(FrameworkElement frameworkElement)
        {
            Style style = frameworkElement.Style;
            if (style != null)
            {
                // check the resource dictionary on the target FrameworkElement first.
                string name = FindNameFromResource(frameworkElement.Resources, style);
                if (name != null)
                    return name;

                // get the parent of the target and check its resource dictionary
                // if not found, continue traveling up the hierarchy and checking
                DependencyObject d = VisualTreeHelper.GetParent(frameworkElement);
                while (d != null)
                {
                    FrameworkElement fe = d as FrameworkElement;
                    if (fe != null)
                    {
                        name = FindNameFromResource(fe.Resources, style);
                    }
                    if (name != null)
                    {
                        return name;
                    }

                    if (fe != null && fe.Parent != null)
                    {
                        d = fe.Parent;
                    }
                    else
                    {
                        d = VisualTreeHelper.GetParent(d);
                    }
                }

                // check the application resources
                if (Application.Current != null)
                {
                    name = FindNameFromResource(Application.Current.Resources, style);
                    if (name != null)
                        return name;
                }
            }
            return string.Empty;
        }

        public static string FindNameFromResource(ResourceDictionary dictionary, object resourceItem)
        {
            foreach (object key in dictionary.Keys)
            {
                if (dictionary[key] == resourceItem)
                {
                    return key.ToString();
                }
            }

            if (dictionary.MergedDictionaries != null)
            {
                foreach (var dic in dictionary.MergedDictionaries)
                {
                    string name = FindNameFromResource(dic, resourceItem);
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
