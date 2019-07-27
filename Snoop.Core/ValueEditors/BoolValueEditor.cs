// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System.Windows.Controls;

namespace Snoop
{
	public partial class BoolValueEditor : ValueEditor
	{
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var cb = Template.FindName( "PART_CheckBox", this ) as CheckBox;
			if ( cb != null )
			{
				cb.Click += CheckBoxClickedHandler;
			}
		}

		private void CheckBoxClickedHandler( object sender, RoutedEventArgs e )
		{
			if ( PropertyInfo != null )
			{
				PropertyInfo.IsValueChangedByUser = true;
			}
		}
	}
}
