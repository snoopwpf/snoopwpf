namespace Snoop.Data.Tree;

using System.Windows;
using System.Windows.Controls.Primitives;

public class PopupTreeItem : DependencyObjectTreeItem
{
    public PopupTreeItem(Popup target, TreeItem? parent, TreeService treeService)
        : base(target, parent, treeService)
    {
        this.TypedTarget = target;
    }

    public Popup TypedTarget { get; }

    protected override void ReloadCore()
    {
        base.ReloadCore();

        if (this.TreeService.TreeType is TreeType.Visual)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(this.TypedTarget))
            {
                if (child is null)
                {
                    continue;
                }

                this.AddChild(this.TreeService.Construct(child, this));
            }
        }
    }
}