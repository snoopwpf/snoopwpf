namespace Snoop {
	using System.Windows.Controls;

	public class Inspector: Grid {
		private PropertyFilter filter;

		public PropertyFilter Filter {
			get { return this.filter; }
			set { 
				this.filter = value;
				this.OnFilterChanged();
			}
		}

		protected virtual void OnFilterChanged() {
		}
	}
}
