namespace Snoop.Data.Tree
{
    using System.Collections.Generic;
    using System.Windows.Automation.Peers;

    public class AutomationPeerTreeItem : TreeItem
    {
        public AutomationPeerTreeItem(AutomationPeer target, TreeItem parent, TreeService treeService)
            : base(target, parent, treeService)
        {
        }

        protected override void Reload(List<TreeItem> toBeRemoved)
        {
            base.Reload(toBeRemoved);

            // remove items that are no longer in tree, add new ones.
            foreach (var child in this.TreeService.GetChildren(this))
            {
                if (child is null)
                {
                    continue;
                }

                var foundItem = false;
                foreach (var item in toBeRemoved)
                {
                    if (ReferenceEquals(item.Target, child))
                    {
                        toBeRemoved.Remove(item);
                        item.Reload();
                        foundItem = true;
                        break;
                    }
                }

                if (foundItem == false)
                {
                    this.Children.Add(this.TreeService.Construct(child, this));
                }
            }
        }
    }
}