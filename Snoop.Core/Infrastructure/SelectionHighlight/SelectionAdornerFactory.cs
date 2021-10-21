namespace Snoop.Infrastructure.SelectionHighlight
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;

    public static class SelectionAdornerFactory
    {
        public static Adorner? CreateAndAttachSelectionAdorner(DependencyObject dependencyObject)
        {
            var uiElement = FindUIElement(dependencyObject);

            if (uiElement is null)
            {
                return null;
            }

            var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);

            if (adornerLayer is null)
            {
                return null;
            }

            var container = CreateSelectionAdorner(uiElement, adornerLayer);

            adornerLayer.Add(container);
            return container;
        }

        public static UIElement? FindUIElement(DependencyObject dependencyObject)
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

        public static Adorner CreateSelectionAdorner(UIElement uiElement, AdornerLayer? adornerLayer)
        {
            return new SelectionAdorner(uiElement)
            {
                AdornerLayer = adornerLayer
            };
        }
    }
}