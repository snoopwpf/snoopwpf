namespace Snoop.Data.Tree;

using System.Windows;
using Snoop.Infrastructure;

public class ChildWindowsTreeItem : TreeItem
{
    private readonly Window targetWindow;

    public ChildWindowsTreeItem(Window target, TreeItem parent, TreeService treeService)
        : base(target, parent, treeService)
    {
        this.targetWindow = target;

        this.ShouldBeAnalyzed = false;
    }

    protected override void ReloadCore()
    {
        base.ReloadCore();

        foreach (Window? ownedWindow in this.targetWindow.OwnedWindows)
        {
            if (ownedWindow is null)
            {
                continue;
            }

            if (ownedWindow.IsInitialized == false
                || ownedWindow.CheckAccess() == false
                || ownedWindow.IsPartOfSnoopVisualTree())
            {
                continue;
            }

            this.AddChild(this.TreeService.Construct(ownedWindow, this));
        }
    }

    public override string ToString()
    {
        return $"{this.Children.Count} child windows";
    }
}