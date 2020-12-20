// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media;
    using Snoop.AttachedProperties;

    /// <summary>
    ///     This service allows Snoop to mark certain visuals as visual tree roots of its own UI.
    /// </summary>
    public static class SnoopPartsRegistry
    {
        private static readonly List<WeakReference> registeredSnoopVisualTreeRoots = new();

        /// <summary>
        /// Checks whether given visual is a part of Snoop's visual tree.
        /// </summary>
        /// <param name="visual">Visual under question</param>
        /// <returns><c>true</c> if <paramref name="visual"/> belongs to Snoop's visual tree. <c>false</c> otherwise.</returns>
        public static bool IsPartOfSnoopVisualTree(this Visual? visual)
        {
            if (visual is null)
            {
                return false;
            }

            if (SnoopAttachedProperties.GetIsSnoopPart(visual))
            {
                return true;
            }

            foreach (var registeredSnoopVisual in registeredSnoopVisualTreeRoots.ToList())
            {
                if (registeredSnoopVisual.IsAlive == false)
                {
                    registeredSnoopVisualTreeRoots.Remove(registeredSnoopVisual);
                    continue;
                }

                var snoopVisual = (Visual?)registeredSnoopVisual.Target;

                if (snoopVisual is null)
                {
                    continue;
                }

                if (ReferenceEquals(visual, snoopVisual)
                    || (visual.Dispatcher == snoopVisual.Dispatcher && visual.IsDescendantOf(snoopVisual)))
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
            if (registeredSnoopVisualTreeRoots.Any(x => x.IsAlive && ReferenceEquals(x.Target, root)) == false)
            {
                registeredSnoopVisualTreeRoots.Add(new WeakReference(root));
            }
        }

        /// <summary>
        ///     Opts out given visual from being considered as a Snoop's visual tree root.
        /// </summary>
        internal static void RemoveSnoopVisualTreeRoot(Visual root)
        {
            var toRemove = registeredSnoopVisualTreeRoots.FirstOrDefault(x => x.IsAlive && ReferenceEquals(x.Target, root));

            if (toRemove is not null)
            {
                registeredSnoopVisualTreeRoots.Remove(toRemove);
            }
        }
    }
}