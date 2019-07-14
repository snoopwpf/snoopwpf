// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Snoop.Infrastructure
{
	/// <summary>
	/// This service allows Snoop to mark certain visuals as visual tree roots of its own UI.
	/// </summary>
	public static class SnoopPartsRegistry
	{
		/// <summary>
		/// Checks whether given visual is a part of Snoop's visual tree.
		/// </summary>
		/// <param name="visual">Visual under question</param>
		/// <returns>True if visual belongs to the Snoop's visual tree; False otherwise.</returns>
		public static bool IsPartOfSnoopVisualTree(this Visual visual)
		{
			if (visual == null) return false;

			foreach (var snoopVisual in _registeredSnoopVisualTreeRoots)
			{
				if
				(
					visual == snoopVisual ||
					(
						visual.Dispatcher == snoopVisual.Dispatcher &&
						visual.IsDescendantOf(snoopVisual)
					)
				)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Adds given visual as a root of Snoop visual tree.
		/// </summary>
		internal static void AddSnoopVisualTreeRoot(Visual root)
		{
			if (!_registeredSnoopVisualTreeRoots.Contains(root))
			{
				_registeredSnoopVisualTreeRoots.Add(root);
			}
		}
		/// <summary>
		/// Opts out given visual from being considered as a Snoop's visual tree root.
		/// </summary>
		internal static void RemoveSnoopVisualTreeRoot(Visual root)
		{
			_registeredSnoopVisualTreeRoots.Remove(root);
		}

		private static List<Visual> _registeredSnoopVisualTreeRoots = new List<Visual>();
	}
}
