// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
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
