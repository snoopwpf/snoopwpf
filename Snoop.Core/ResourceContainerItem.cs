// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Collections.Generic;

namespace Snoop
{
	public abstract class ResourceContainerItem : VisualTreeItem
	{
		public ResourceContainerItem(object target, VisualTreeItem parent): base(target, parent)
		{
		}

		protected abstract ResourceDictionary ResourceDictionary { get; }

		protected override void Reload(List<VisualTreeItem> toBeRemoved)
		{
			base.Reload(toBeRemoved);

			ResourceDictionary resources = this.ResourceDictionary;

			if (resources != null && resources.Count != 0)
			{
				bool foundItem = false;
				foreach (VisualTreeItem item in toBeRemoved)
				{
					if (item.Target == resources)
					{
						toBeRemoved.Remove(item);
						item.Reload();
						foundItem = true;
						break;
					}
				}
				if (!foundItem)
					this.Children.Add(VisualTreeItem.Construct(resources, this));
			}
		}
	}
}
