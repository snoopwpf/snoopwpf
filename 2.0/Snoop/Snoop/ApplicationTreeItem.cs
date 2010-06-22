using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;

namespace Snoop {
	public class ApplicationTreeItem: ResourceContainerItem {

		private Application application;

		public ApplicationTreeItem(Application application, VisualTreeItem parent): base(application, parent) {
			this.application = application;
		}

		public override Visual MainVisual {
			get {
				return this.application.MainWindow;
			}
		}

		protected override void Reload(List<VisualTreeItem> toBeRemoved) {
			base.Reload(toBeRemoved);

			if (this.application.MainWindow != null) {
				bool foundMainWindow = false;
				foreach (VisualTreeItem item in toBeRemoved) {
					if (item.Target == this.application.MainWindow) {
						toBeRemoved.Remove(item);
						item.Reload();
						foundMainWindow = true;
						break;
					}
				}

				if (!foundMainWindow)
					this.Children.Add(VisualTreeItem.Construct(this.application.MainWindow, this));
			}
		}

		protected override ResourceDictionary ResourceDictionary {
			get { return this.application.Resources; }
		}

	}
}
