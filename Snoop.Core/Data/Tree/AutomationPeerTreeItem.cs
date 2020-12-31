namespace Snoop.Data.Tree
{
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Snoop.Controls;

    public class AutomationPeerTreeItem : TreeItem
    {
        private AdornerContainer? adornerContainer;

        public AutomationPeerTreeItem(AutomationPeer target, TreeItem? parent, TreeService treeService)
            : base(target, parent, treeService)
        {
            if (this.Target is UIElementAutomationPeer { Owner: { } } uiElementAutomationPeer)
            {
                this.Visual = uiElementAutomationPeer.Owner;
            }
        }

        public Visual? Visual { get; }

        protected override void ReloadCore()
        {
            if (this.Visual is not null)
            {
                // We just want to include the owner as a tree item, but not it's children
                this.AddChild(this.TreeService.Construct(this.Visual, this, omitChildren: true));
            }

            foreach (var child in this.TreeService.GetChildren(this))
            {
                if (child is null)
                {
                    continue;
                }

                this.AddChild(this.TreeService.Construct(child, this));
            }
        }

        protected override void OnIsSelectedChanged()
        {
            if (this.Visual is null)
            {
                return;
            }

            // Add adorners for the visual this is representing.
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.Visual);

            if (adornerLayer is not null
                && this.Visual is UIElement visualElement)
            {
                if (this.IsSelected
                    && this.adornerContainer is null)
                {
                    var border = new Border
                    {
                        BorderThickness = new Thickness(4),
                        IsHitTestVisible = false
                    };

                    var borderColor = new Color
                    {
                        ScA = .3f,
                        ScR = 1
                    };
                    border.BorderBrush = new SolidColorBrush(borderColor);

                    this.adornerContainer = new AdornerContainer(visualElement)
                    {
                        Child = border
                    };
                    adornerLayer.Add(this.adornerContainer);
                }
                else if (this.adornerContainer is not null)
                {
                    adornerLayer.Remove(this.adornerContainer);
                    this.adornerContainer.Child = null;
                    this.adornerContainer = null;
                }
            }
        }
    }
}