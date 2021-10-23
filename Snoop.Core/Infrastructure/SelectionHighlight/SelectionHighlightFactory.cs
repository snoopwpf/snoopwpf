namespace Snoop.Infrastructure.SelectionHighlight
{
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
            if (dependencyObject is UIElement uiElement)
            {
                return uiElement;
            }

            if (dependencyObject is ColumnDefinition columnDefinition)
            {
                return columnDefinition.Parent as UIElement;
            }

            if (dependencyObject is RowDefinition rowDefinition)
            {
                return rowDefinition.Parent as UIElement;
            }

            if (dependencyObject is ContentElement contentElement)
            {
                return contentElement.GetUIParent() as UIElement ?? contentElement.GetParent() as UIElement;
            }

            return null;
        }

        private static SelectionAdorner CreateSelectionAdorner(UIElement uiElement, AdornerLayer? adornerLayer)
        {
            return new SelectionAdorner(uiElement)
            {
                AdornerLayer = adornerLayer
            };
        }
    }
}