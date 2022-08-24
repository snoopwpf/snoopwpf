// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Snoop.AttachedProperties;
using Snoop.Core;

/// <summary>
///     This service allows Snoop to mark certain visuals as visual tree roots of its own UI.
/// </summary>
public static class SnoopPartsRegistry
{
    private static readonly List<WeakReference<Visual>> registeredSnoopVisualTreeRoots = new();

    public static bool IsSnoopingSnoop { get; set; }

    /// <summary>
    /// Checks whether given visual is a part of Snoop's visual tree.
    /// </summary>
    /// <param name="dependencyObject">DependencyObject under question</param>
    /// <returns><c>true</c> if <paramref name="dependencyObject"/> belongs to Snoop's visual tree. <c>false</c> otherwise.</returns>
    public static bool IsPartOfSnoopVisualTree(this DependencyObject? dependencyObject)
    {
        if (dependencyObject is null
            || IsSnoopingSnoop)
        {
            return false;
        }

        if (SnoopAttachedProperties.GetIsSnoopPart(dependencyObject))
        {
            return true;
        }

        foreach (var registeredSnoopVisual in registeredSnoopVisualTreeRoots.ToList())
        {
            if (registeredSnoopVisual.TryGetTarget(out var snoopVisual) == false)
            {
                registeredSnoopVisualTreeRoots.Remove(registeredSnoopVisual);
                continue;
            }

            if (snoopVisual is null)
            {
                continue;
            }

            if (ReferenceEquals(dependencyObject, snoopVisual)
                || (dependencyObject.Dispatcher == snoopVisual.Dispatcher && (dependencyObject as Visual)?.IsDescendantOf(snoopVisual) == true))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Adds given visual as a root of Snoop visual tree.
    /// </summary>
    internal static void AddSnoopVisualTreeRoot(Visual root)
    {
        if (registeredSnoopVisualTreeRoots.Any(x => x.TryGetTarget(out var target) && ReferenceEquals(target, root)) == false)
        {
            registeredSnoopVisualTreeRoots.Add(new(root));

            //ThemeManager.Current.ApplyTheme(Settings.Default.ThemeMode, root);
        }
    }

    /// <summary>
    ///     Opts out given visual from being considered as a Snoop's visual tree root.
    /// </summary>
    internal static void RemoveSnoopVisualTreeRoot(Visual root)
    {
        var toRemove = registeredSnoopVisualTreeRoots.FirstOrDefault(x => x.TryGetTarget(out var target) && ReferenceEquals(target, root));

        if (toRemove is not null)
        {
            registeredSnoopVisualTreeRoots.Remove(toRemove);
        }
    }

    internal static IEnumerable<Visual> GetSnoopVisualTreeRoots()
    {
        foreach (var weakReference in registeredSnoopVisualTreeRoots)
        {
            if (weakReference.TryGetTarget(out var target))
            {
                yield return target;
            }
        }
    }
}