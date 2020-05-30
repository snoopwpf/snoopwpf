namespace Snoop.Data.Tree
{
    using System.Windows.Automation.Peers;

    public class AutomationPeerTreeItem : TreeItem
    {
        public AutomationPeerTreeItem(AutomationPeer target, TreeItem parent, TreeService treeService)
            : base(target, parent, treeService)
        {
        }

        protected override void ReloadCore()
        {
            // remove items that are no longer in tree, add new ones.
            foreach (var child in this.TreeService.GetChildren(this))
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