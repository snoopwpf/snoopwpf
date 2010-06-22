using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Snoop {
	public class ResourceDictionaryItem: VisualTreeItem {

		private ResourceDictionary dictionary;

		public ResourceDictionaryItem(ResourceDictionary dictionary, VisualTreeItem parent): base(dictionary, parent) {
			this.dictionary = dictionary;
		}

		protected override void Reload(List<VisualTreeItem> toBeRemoved) {
			base.Reload(toBeRemoved);

			foreach (object key in this.dictionary.Keys) {

				object target = this.dictionary[key];

				bool foundItem = false;
				foreach (VisualTreeItem item in toBeRemoved) {
					if (item.Target == target) {
						toBeRemoved.Remove(item);
						item.Reload();
						foundItem = true;
						break;
					}
				}

				if (!foundItem)
					this.Children.Add(new ResourceItem(target, key, this));
			}
		}

		public override string ToString() {
			return this.dictionary.Count + " Resources";
		}
	}

	public class ResourceItem : VisualTreeItem {

		private object key;

		public ResourceItem(object target, object key, VisualTreeItem parent): base(target, parent) {
			this.key = key;
		}

		public override string ToString() {
			return this.key.ToString() + " (" + this.Target.GetType().Name + ")";
		}
	}
}
