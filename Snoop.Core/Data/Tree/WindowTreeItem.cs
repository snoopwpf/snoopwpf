namespace Snoop.Data.Tree
{
    using System.Collections.Generic;
    using System.Windows;
    using Snoop.Infrastructure;

    public class WindowTreeItem : DependencyObjectTreeItem
    {
        public WindowTreeItem(Window target, TreeItem parent, TreeService treeService)
            : base(target, parent, treeService)
        {
        }

        protected override void Reload(List<TreeItem> toBeRemoved)
        {
            if (this.Target is Window window)
            {
                foreach (Window ownedWindow in window.OwnedWindows)
                {
                    if (ownedWindow.IsInitialized == false
                        || ownedWindow.CheckAccess() == false
                        || ownedWindow.IsPartOfSnoopVisualTree())
                    {
                        continue;
                    }

                    var childWindowsTreeItem = new ChildWindowsTreeItem(window, this, this.TreeService);
                    childWindowsTreeItem.Reload();
                    this.Children.Add(childWindowsTreeItem);
                    break;
                }
            }

            base.Reload(toBeRemoved);
        }
    }
}