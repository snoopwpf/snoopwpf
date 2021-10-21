namespace Snoop.Infrastructure.SelectionHighlight
{
    using System.Windows;
    using System.Windows.Documents;

    public static class SelectionAdornerFactory
    {
        public static Adorner? CreateAndAttachSelectionAdorner(UIElement uiElement)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);

            if (adornerLayer is null)
            {
                return null;
            }

            var container = CreateSelectionAdorner(uiElement, adornerLayer);

            adornerLayer.Add(container);
            return container;
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