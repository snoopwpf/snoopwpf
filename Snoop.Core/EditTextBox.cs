// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Snoop
{
	/// <summary>
	/// Allows committing a value when pressing Return, and requerying the value after setting it.
	/// </summary>
	public class EditTextBox : TextBox
	{
		protected override void OnInitialized(System.EventArgs e)
		{
			base.OnInitialized(e);

			// Take focus and select all the text by default.
			// Great for in the property editor, so the text is quick to edit,
			// but probably not useful in general.
			this.Focus();
			this.SelectAll();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(this, EditTextBox.TextProperty);
				if (expression != null)
				{
					expression.UpdateSource();
				}
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}
	}
}
