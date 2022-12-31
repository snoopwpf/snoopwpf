// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls;

using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Snoop.AttachedProperties;

/// <summary>
/// Simple helper class to allow any UIElements to be used as an Adorner.
/// </summary>
public class AdornerContainer : Adorner, IDisposable
{
    private UIElement? child;

    static AdornerContainer()
    {
        IsHitTestVisibleProperty.OverrideMetadata(typeof(AdornerContainer), new UIPropertyMetadata(false));
    }

    public AdornerContainer(UIElement adornedElement)
        : base(adornedElement)
    {
        this.IsHitTestVisible = false;
        SnoopAttachedProperties.SetIsSnoopPart(this, true);
    }

    protected override int VisualChildrenCount => this.child is null ? 0 : 1;

    protected override Visual? GetVisualChild(int index)
    {
        if (index == 0
            && this.child is not null)
        {
            return this.child;
        }

        return base.GetVisualChild(index);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        this.child?.Arrange(new Rect(finalSize));

        return finalSize;
    }

    public UIElement? Child
    {
        get { return this.child; }

        set
        {
            this.AddVisualChild(value);
            this.child = value;
        }
    }

    public AdornerLayer? AdornerLayer { get; set; }

    public void Dispose()
    {
        this.Child = null;
        this.AdornerLayer?.Remove(this);
    }
}