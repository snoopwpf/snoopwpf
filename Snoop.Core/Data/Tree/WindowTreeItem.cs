namespace Snoop.Data.Tree;

using System.Windows;
using Snoop.Infrastructure;

public class WindowTreeItem : DependencyObjectTreeItem
{
    public WindowTreeItem(Window target, TreeItem? parent, TreeService treeService, bool isScoped)
        : base(target, parent, treeService, isScoped)
    {
        this.WindowTarget = target;
    }

    public Window WindowTarget { get; }

    protected override void ReloadCore()
    {
        foreach (Window? ownedWindow in this.WindowTarget.OwnedWindows)
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

            var childWindowsTreeItem = new ChildWindowsTreeItem(this.WindowTarget, this, this.TreeService);
            childWindowsTreeItem.Reload();
            this.AddChild(childWindowsTreeItem);
            break;
        }

        base.ReloadCore();
    }
}