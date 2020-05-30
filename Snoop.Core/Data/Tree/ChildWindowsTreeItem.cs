namespace Snoop.Data.Tree
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Snoop.Infrastructure;

    public class ChildWindowsTreeItem : TreeItem
    {
        private readonly Window targetWindow;

        public ChildWindowsTreeItem(Window target, TreeItem parent, TreeService treeService)
            : base(target, parent, treeService)
        {
            this.targetWindow = target;
        }

        protected override void Reload(List<TreeItem> toBeRemoved)
        {
            base.Reload(toBeRemoved);

            foreach (Window ownedWindow in this.targetWindow.OwnedWindows)
            {
                if (ownedWindow.IsInitialized == false
                    || ownedWindow.CheckAccess() == false
                    || ownedWindow.IsPartOfSnoopVisualTree())
                {
                    continue;
                }

                // don't recreate existing items but reload them instead
                var existingItem = toBeRemoved.FirstOrDefault(x => ReferenceEquals(x.Target, ownedWindow));
                if (existingItem != null)
                {
                    toBeRemoved.Remove(existingItem);
                    existingItem.Reload();
                    continue;
                }

                this.Children.Add(this.TreeService.Construct(ownedWindow, this));
            }
        }

        public override string ToString()
        {
            return $"{this.Children.Count} child windows";
        }
    }
}