namespace Snoop.Data.Tree
{
    using System.Windows;
    using System.Windows.Controls.Primitives;

    public class PopupTreeItem : DependencyObjectTreeItem
    {
        public PopupTreeItem(Popup target, TreeItem? parent, TreeService treeService)
            : base(target, parent, treeService)
        {
            this.PopupTarget = target;
        }

        public Popup PopupTarget { get; }

        protected override void ReloadCore()
        {
            base.ReloadCore();

            if (this.TreeService.TreeType == TreeType.Visual)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(this.PopupTarget))
                {
                    if (child is null)
                    {
                        continue;
                    }

                    this.Children.Add(this.TreeService.Construct(child, this));
                }
            }
        }
    }
}