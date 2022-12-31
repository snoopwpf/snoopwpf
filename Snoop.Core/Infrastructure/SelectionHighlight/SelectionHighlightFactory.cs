namespace Snoop.Infrastructure.SelectionHighlight;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

public static class SelectionHighlightFactory
{
    public static IDisposable? CreateAndAttachSelectionHighlight(DependencyObject dependencyObject)
    {
        var uiElement = FindUIElement(dependencyObject);

        if (uiElement is null)
        {
            return null;
        }

        return CreateAndAttachSelectionHighlightAdorner(uiElement);
    }

    private static IDisposable? CreateAndAttachSelectionHighlightAdorner(UIElement uiElement)
    {
        var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);

        if (adornerLayer is null)
        {
            return null;
        }

        var selectionAdorner = CreateSelectionAdorner(uiElement, adornerLayer);

        adornerLayer.Add(selectionAdorner);
        return selectionAdorner;
    }

    private static UIElement? FindUIElement(DependencyObject dependencyObject)
    {
        return dependencyObject switch
        {
            UIElement uiElement => uiElement,
            ColumnDefinition columnDefinition => columnDefinition.Parent as UIElement,
            RowDefinition rowDefinition => rowDefinition.Parent as UIElement,
            ContentElement contentElement => contentElement.GetUIParent() as UIElement ?? contentElement.GetParent() as UIElement,
            _ => null
        };
    }

    private static SelectionAdorner CreateSelectionAdorner(UIElement uiElement, AdornerLayer? adornerLayer)
    {
        return new SelectionAdorner(uiElement)
        {
            AdornerLayer = adornerLayer
        };
    }
}