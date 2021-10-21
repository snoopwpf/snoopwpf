namespace Snoop.Data.Tree
{
    using System;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Snoop.Infrastructure.SelectionHighlight;

    public class AutomationPeerTreeItem : TreeItem
    {
        private Adorner? selectionHighlight;

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
            // Add adorners for the visual this is representing.
            if (this.Target is DependencyObject dependencyObject)
            {
                if (this.IsSelected
                    && this.selectionHighlight is null)
                {
                    this.selectionHighlight = SelectionAdornerFactory.CreateAndAttachSelectionAdorner(dependencyObject);
                }
                else if (this.selectionHighlight is not null)
                {
                    (this.selectionHighlight as IDisposable)?.Dispose();
                    this.selectionHighlight = null;
                }
            }
        }
    }
}