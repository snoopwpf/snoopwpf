// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System.Collections.Generic;
    using System.Windows;

    public abstract class ResourceContainerItem : VisualTreeItem
    {
        protected ResourceContainerItem(object target, VisualTreeItem parent)
            : base(target, parent)
        {
        }

        protected abstract ResourceDictionary ResourceDictionary { get; }

        protected override void Reload(List<VisualTreeItem> toBeRemoved)
        {
            base.Reload(toBeRemoved);

            var resources = this.ResourceDictionary;

            if (resources != null && resources.Count != 0)
            {
                var foundItem = false;
                foreach (var item in toBeRemoved)
                {
                    if (item.Target == resources)
                    {
                        toBeRemoved.Remove(item);
                        item.Reload();
                        foundItem = true;
                        break;
                    }
                }

                if (foundItem == false)
                {
                    this.Children.Add(Construct(resources, this));
                }
            }
        }
    }
}