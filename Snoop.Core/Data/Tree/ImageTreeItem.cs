namespace Snoop.Data.Tree;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

public class ImageTreeItem : DependencyObjectTreeItem
{
    public ImageTreeItem(Image target, TreeItem? parent, TreeService treeService)
        : base(target, parent, treeService)
    {
        this.TypedTarget = target;
    }

    public Image TypedTarget { get; }

    protected override void ReloadCore()
    {
        base.ReloadCore();

        if (this.TreeService.TreeType is TreeType.Visual or TreeType.Logical
            && this.TypedTarget.Source is not null)
        {
            this.AddChild(this.TreeService.Construct(this.TypedTarget.Source, this));
        }
    }
}