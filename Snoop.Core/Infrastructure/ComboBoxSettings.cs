// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Snoop.Infrastructure
{
	/// <summary>
	/// Attached behavior that allows Snoop to distinguish its own ComboBoxes 
	/// and ignore routed events from their popups.
	/// </summary>
	internal sealed class ComboBoxSettings
	{
		/// <summary>
		/// Indicates whether given ComboBox is a part of the Snoop UI.
		/// If ComboBox is a part of Snoop UI it doesn't take part in
		/// routed events monitoring.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static bool GetIsSnoopPart(ComboBox obj)
		{
			return (bool)obj.GetValue(IsSnoopPartProperty);
		}
		/// <summary>
		/// Allows a ComboBox opt in/opt out from being a part of Snoop UI.
		/// If ComboBox is a part of Snoop UI it doesn't take part in 
		/// routed events monitoring.
		/// </summary>
		public static void SetIsSnoopPart(ComboBox obj, bool value)
		{
			obj.SetValue(IsSnoopPartProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsSnoopPart"/> attached property.
		/// </summary>
		public static readonly DependencyProperty IsSnoopPartProperty =
			DependencyProperty.RegisterAttached
			(
				"IsSnoopPart",
				typeof(bool),
				typeof(ComboBox),
				new PropertyMetadata(false, OnIsSnoopPartChanged)
			);
		private static void OnIsSnoopPartChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{			
			var cb = o as ComboBox;
			if (cb != null)
			{
				cb.WhenLoaded(fe => UpdateSnoopPartSettings(cb, (bool)e.NewValue));
			}
		}


		private static void UpdateSnoopPartSettings(ComboBox comboBox, bool isSnoopPart)
		{
			var popup = GetComboBoxPopup(comboBox);
			if (popup == null)
			{
				return;
			}

			if (isSnoopPart)
			{
				RegisterAsSnoopPopup(popup);
			}
			else
			{
				UnregisterAsSnoopPopup(popup);
			}
		}

		private static Popup GetComboBoxPopup(ComboBox comboBox)
		{
			if (comboBox == null || comboBox.Template == null)
			{
				return null;
			}
			comboBox.ApplyTemplate();
			return comboBox.Template.FindName(PopupTemplateName, comboBox) as Popup;
		}

		private static void RegisterAsSnoopPopup(Popup popup)
		{
			popup.Opened += SnoopChildPopupOpened;
			popup.Closed += SnoopChildPopupClosed;
		}
		private static void UnregisterAsSnoopPopup(Popup popup)
		{
			popup.Opened -= SnoopChildPopupOpened;
			popup.Closed -= SnoopChildPopupClosed;
		}

		private static void SnoopChildPopupOpened(object sender, EventArgs e)
		{
			var popup = (Popup)sender;
			if (popup.Child != null)
			{
				// Cannot use 'popup' as a snoop part, since it's not
				// going to be in the PopupRoot's visual tree. The closest
				// object in the PopupRoot's tree is popup.Child:
				SnoopPartsRegistry.AddSnoopVisualTreeRoot(popup.Child);
			}
		}
		private static void SnoopChildPopupClosed(object sender, EventArgs e)
		{
			var popup = (Popup)sender;
			if (popup.Child != null)
			{
				// Cannot use 'popup' as a snoop part, since it's not
				// going to be in the PopupRoot's visual tree. The closest
				// object in the PopupRoot's tree is popup.Child:
				SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(popup.Child);
			}
		}

		private const string PopupTemplateName = "PART_Popup";
	}
}
