// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using Snoop.Infrastructure;

namespace Snoop
{
	/// <summary>
	/// Exposes attached behaviors that brings given FrameworkElement into view.
	/// </summary>
	public static class BringIntoViewBehavior
	{
		public static bool GetIsActive(FrameworkElement fe)
		{
			return (bool)fe.GetValue(IsActiveProperty);
		}
		public static void SetIsActive(FrameworkElement fe, bool value)
		{
			fe.SetValue(IsActiveProperty, value);
		}
		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.RegisterAttached
			(
				"IsActive",
				typeof(bool),
				typeof(BringIntoViewBehavior),
				new UIPropertyMetadata(false, OnIsActiveChanged)
			);

		private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var fe = d as FrameworkElement;
			if (fe == null)
			{
				return;
			}

			if ((bool)e.NewValue)
			{
				// Can Bring into view only when element is loaded.
				fe.WhenLoaded
				(
					element =>
					{
						if (GetIsActive(element))
						{
							element.BringIntoView();
						}
					}
				);
			}
		}
	}
}
