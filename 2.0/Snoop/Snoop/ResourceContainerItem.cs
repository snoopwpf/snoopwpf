using System.Windows;
using System.Collections.Generic;
namespace Snoop {
	public abstract class ResourceContainerItem: VisualTreeItem {

		public ResourceContainerItem(object target, VisualTreeItem parent): base(target, parent) {
		}

		protected override void Reload(List<VisualTreeItem> toBeRemoved) {
			base.Reload(toBeRemoved);

			ResourceDictionary resources = this.ResourceDictionary;

			if (resources != null && resources.Count != 0) {
				bool foundItem = false;
				foreach (VisualTreeItem item in toBeRemoved) {
					if (item.Target == resources) {
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

		protected abstract ResourceDictionary ResourceDictionary { get; }
	}
}
