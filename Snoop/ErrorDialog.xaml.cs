// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Text;
using System.Windows;
using Snoop.Infrastructure;

namespace Snoop
{
	public partial class ErrorDialog
	{
		public ErrorDialog()
		{
			this.InitializeComponent();

			this.Loaded += ErrorDialog_Loaded;
			this.Closed += ErrorDialog_Closed;
		}

		public Exception Exception { get; set; }

	    public static bool ShowDialog(Exception exception)
	    {
	        // should we check if the exception came from Snoop? perhaps seeing if any Snoop call is in the stack trace?
	        var dialog = new ErrorDialog
	                     {
	                         Exception = exception
	                     };
	        var result = dialog.ShowDialog();
	        if (result.HasValue 
	            && result.Value)
	        {
	            return true;
	        }

	        return false;
	    }

	    public static void ShowExceptionMessageBox(Exception exception, string title = "Exception", string message = "")
	    {
	        var finalMessage = message + $"\nException:\n{exception}";
	        MessageBox.Show(finalMessage.TrimStart(), title, MessageBoxButton.OK, MessageBoxImage.Error);
	    }

	    private void ErrorDialog_Loaded(object sender, RoutedEventArgs e)
		{
			this._textBlockException.Text = this.GetExceptionMessage();

			SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);
		}

		private void ErrorDialog_Closed(object sender, EventArgs e)
		{
			SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
		}

		private void _buttonCopyToClipboard_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Clipboard.SetText(this.GetExceptionMessage());
			}
			catch (Exception exception)
			{
			    ShowExceptionMessageBox(exception, "Error copying to clipboard", "There was an error copying to the clipboard.\nPlease copy the exception from the above textbox manually.");
			}
		}
		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
			}
			catch (Exception exception)
			{
				var message = $"There was an error starting the browser. Please visit \"{e.Uri.AbsoluteUri}\" manually to create an issue.";
			    ShowExceptionMessageBox(exception, "Error starting browser", message);
			}
		}

		private void CloseDoNotMarkHandled_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			if (CheckBoxRememberIsChecked())
			{
				SnoopModes.IgnoreExceptions = true;
			}
			this.Close();
		}
		private void CloseAndMarkHandled_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;

			if (CheckBoxRememberIsChecked())
			{
				SnoopModes.SwallowExceptions = true;
			}

			this.Close();
		}

		private string GetExceptionMessage()
		{
			StringBuilder builder = new StringBuilder();
			GetExceptionString(this.Exception, builder);
			return builder.ToString();
		}

		private static void GetExceptionString(Exception exception, StringBuilder builder, bool isInner = false)
		{
			if (exception == null)
				return;

			if (isInner)
				builder.AppendLine("\n\nInnerException:\n");

			builder.AppendLine(string.Format("Message: {0}", exception.Message));
			builder.AppendLine(string.Format("Stacktrace:\n{0}", exception.StackTrace));

			GetExceptionString(exception.InnerException, builder, true);
		}

		private bool CheckBoxRememberIsChecked()
		{
			return this._checkBoxRemember.IsChecked.HasValue && this._checkBoxRemember.IsChecked.Value;
		}
	}
}
