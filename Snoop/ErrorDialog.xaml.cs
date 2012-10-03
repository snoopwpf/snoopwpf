// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Snoop.Infrastructure;

namespace Snoop
{
	/// <summary>
	/// Interaction logic for ErrorDialog.xaml
	/// </summary>
	public partial class ErrorDialog : Window
	{
		public ErrorDialog()
		{
			InitializeComponent();

			this.Loaded += ErrorDialog_Loaded;
			this.Closed += ErrorDialog_Closed;
		}

		public Exception Exception { get; set; }

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
			catch (Exception ex)
			{
				string message = string.Format("There was an error copying to the clipboard:\nMessage = {0}\n\nPlease copy the exception from the above textbox manually!", ex.Message);
				MessageBox.Show(message, "Error copying to clipboard");
			}
		}
		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
			}
			catch (Exception)
			{
				string message = string.Format("There was an error starting the browser. Please visit \"{0}\" to create the issue.", e.Uri.AbsoluteUri);
				MessageBox.Show(message, "Error starting browser");
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
