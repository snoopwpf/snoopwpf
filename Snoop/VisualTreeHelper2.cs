// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Media;

namespace Snoop
{
	public static class VisualTreeHelper2
	{
		public delegate TResult Func<T1, T2, TResult>(T1 v1, T2 v2);
		public delegate HitTestFilterBehavior EnumerateTreeFilterCallback(Visual visual, object misc);
		public delegate HitTestResultBehavior EnumerateTreeResultCallback(Visual visual, object misc);

		public static bool IsFrameworkElementName(Visual visual, object name)
		{
			FrameworkElement element = visual as FrameworkElement;
			return element != null && string.CompareOrdinal(element.Name, (string)name) == 0;
		}
		public static bool IsFrameworkElementTemplatedChild(Visual visual, object templatedParent)
		{
			FrameworkElement element = visual as FrameworkElement;
			return element != null && element.TemplatedParent == (DependencyObject)templatedParent;
		}

		public static void EnumerateTree(Visual reference, EnumerateTreeFilterCallback filterCallback, EnumerateTreeResultCallback enumeratorCallback, object misc)
		{
			if (reference == null)
			{
				throw new ArgumentNullException("reference");
			}
			else
			{
				DoEnumerateTree(reference, filterCallback, enumeratorCallback, misc);
			}
		}

		public static T GetAncestor<T>(Visual cur, Visual root, Func<Visual, object, bool> predicate, object param)
			where T : Visual
		{
			T result = cur as T;
			while (cur != null && cur != root && (result == null || (predicate != null && !predicate(result, param))))
			{
				cur = (Visual)VisualTreeHelper.GetParent(cur);
				result = cur as T;
			}
			return result;
		}
		public static T GetAncestor<T>(Visual cur, Visual root, Func<Visual, object, bool> predicate) where T : Visual
		{
			return GetAncestor<T>(cur, root, predicate, null);
		}
		public static T GetAncestor<T>(Visual cur, Visual root) where T : Visual
		{
			return GetAncestor<T>(cur, root, null, null);
		}
		public static T GetAncestor<T>(Visual cur) where T : Visual
		{
			return GetAncestor<T>(cur, null, null, null);
		}

		private static bool DoEnumerateTree(Visual reference, EnumerateTreeFilterCallback filterCallback, EnumerateTreeResultCallback enumeratorCallback, object misc)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(reference); ++i)
			{
				Visual child = (Visual)VisualTreeHelper.GetChild(reference, i);

				HitTestFilterBehavior filterResult = HitTestFilterBehavior.Continue;
				if (filterCallback != null)
				{
					filterResult = filterCallback(child, misc);
				}

				bool enumerateSelf = true;
				bool enumerateChildren = true;

				switch (filterResult)
				{
					case HitTestFilterBehavior.Continue:
						break;
					case HitTestFilterBehavior.ContinueSkipChildren:
						enumerateChildren = false;
						break;
					case HitTestFilterBehavior.ContinueSkipSelf:
						enumerateSelf = false;
						break;
					case HitTestFilterBehavior.ContinueSkipSelfAndChildren:
						enumerateChildren = false;
						enumerateSelf = false;
						break;
					default:
						return false;
				}

				if
				(
					(enumerateSelf && enumeratorCallback != null && enumeratorCallback(child, misc) == HitTestResultBehavior.Stop) ||
					(enumerateChildren && !DoEnumerateTree(child, filterCallback, enumeratorCallback, misc))
				)
				{
					return false;
				}
			}

			return true;
		}
	}
}
