using System;
using System.Windows;

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
				new WhenLoaded
				(
					fe,
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

	/// <summary>
	/// Executes action on framework element when it's loaded.
	/// </summary>
	public class WhenLoaded
	{
		public WhenLoaded(FrameworkElement target, Action<FrameworkElement> loadedAction)
		{
			this.target = target;
			this.loadedAction = loadedAction;

			DoAction();
		}

		private void DoAction()
		{
			if (this.target.IsLoaded)
			{
				this.loadedAction(this.target);
			}
			else
			{
				this.target.Loaded += TargetLoaded;
			}
		}

		private void TargetLoaded(object sender, RoutedEventArgs e)
		{
			this.target.Loaded -= TargetLoaded;
			this.loadedAction(this.target);
		}

		private readonly FrameworkElement target;
		private readonly Action<FrameworkElement> loadedAction;
	}
}
