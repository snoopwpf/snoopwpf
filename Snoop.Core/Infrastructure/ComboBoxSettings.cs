// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

/// <summary>
/// Attached behavior that allows Snoop to distinguish its own ComboBoxes 
/// and ignore routed events from their popups.
/// </summary>
internal static class ComboBoxSettings
{
    /// <summary>
    /// Indicates whether given ComboBox is a part of the Snoop UI.
    /// If ComboBox is a part of Snoop UI it doesn't take part in
    /// routed events monitoring.
    /// </summary>
    /// <returns></returns>
    public static bool GetRegisterAsSnoopPart(ComboBox obj)
    {
        return (bool)obj.GetValue(RegisterAsSnoopPartProperty);
    }

    /// <summary>
    /// Allows a ComboBox opt in/opt out from being a part of Snoop UI.
    /// If ComboBox is a part of Snoop UI it doesn't take part in 
    /// routed events monitoring.
    /// </summary>
    public static void SetRegisterAsSnoopPart(ComboBox obj, bool value)
    {
        obj.SetValue(RegisterAsSnoopPartProperty, value);
    }

    /// <summary>
    /// Identifies the "RegisterAsSnoopPart" attached property.
    /// </summary>
    public static readonly DependencyProperty RegisterAsSnoopPartProperty =
        DependencyProperty.RegisterAttached(
            "RegisterAsSnoopPart",
            typeof(bool),
            typeof(ComboBoxSettings),
            new PropertyMetadata(false, OnRegisterAsSnoopPartChanged));

    private static void OnRegisterAsSnoopPartChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var comboBox = o as ComboBox;
        comboBox?.WhenLoaded(_ => UpdateSnoopPartSettings(comboBox, (bool)e.NewValue));
    }

    private static void UpdateSnoopPartSettings(ComboBox comboBox, bool isSnoopPart)
    {
        var popup = GetComboBoxPopup(comboBox);

        if (popup is null)
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

    private static Popup? GetComboBoxPopup(ComboBox comboBox)
    {
        if (comboBox?.Template is null)
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

    private static void SnoopChildPopupOpened(object? sender, EventArgs e)
    {
        var popup = (Popup?)sender;
        if (popup?.Child is not null)
        {
            // Cannot use 'popup' as a snoop part, since it's not
            // going to be in the PopupRoot's visual tree. The closest
            // object in the PopupRoot's tree is popup.Child:
            SnoopPartsRegistry.AddSnoopVisualTreeRoot(popup.Child);
        }
    }

    private static void SnoopChildPopupClosed(object? sender, EventArgs e)
    {
        var popup = (Popup?)sender;
        if (popup?.Child is not null)
        {
            // Cannot use 'popup' as a snoop part, since it's not
            // going to be in the PopupRoot's visual tree. The closest
            // object in the PopupRoot's tree is popup.Child:
            SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(popup.Child);
        }
    }

    private const string PopupTemplateName = "PART_Popup";
}