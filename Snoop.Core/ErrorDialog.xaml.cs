// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using Snoop.Infrastructure;

namespace Snoop
{
    public partial class ErrorDialog
	{
	    private bool exceptionAlreadyHandled;

		public ErrorDialog()
		{
			this.InitializeComponent();

			this.Loaded += this.ErrorDialog_Loaded;
			this.Closed += this.ErrorDialog_Closed;
		}

		public Exception Exception { get; private set; }

	    public bool ExceptionAlreadyHandled
	    {
	        get => this.exceptionAlreadyHandled;
	        private set 
	        { 
	            this.exceptionAlreadyHandled = value;
	            this.HandledExceptionPanel.Visibility = value
	                                                        ? Visibility.Visible
	                                                        : Visibility.Collapsed;

	            this.UnhandledExceptionPanel.Visibility = value
	                                                        ? Visibility.Collapsed
	                                                        : Visibility.Visible;
	        }
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <returns><c>true</c> is the exception should be marked handled and <c>false</c> if the exception should NOT be </returns>
	    public static bool ShowDialog(Exception exception, string title = "Error occurred", string caption = "An error has occured", bool exceptionAlreadyHandled = false)
	    {
	        // should we check if the exception came from Snoop? perhaps seeing if any Snoop call is in the stack trace?
	        var dialog = new ErrorDialog
	                     {
	                         Title = title + " - Snoop",
	                         captionTextBlock =
	                         {
	                             Text = caption
	                         },
	                         Exception = exception,
                             ExceptionAlreadyHandled = exceptionAlreadyHandled
	                     };

	        var result = dialog.ShowDialog();

	        return (result ?? false) == false;
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
			ClipboardHelper.SetText(this.GetExceptionMessage());
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			try
			{
			    var body = this.GenerateIssueBody();
                
			    var titleParameter = Uri.EscapeDataString(this.Exception.Message);
			    var bodyParameter = Uri.EscapeDataString(body);
				System.Diagnostics.Process.Start($"https://github.com/cplotts/snoopwpf/issues/new?title={titleParameter}&body={bodyParameter}");
			}
			catch (Exception exception)
			{
				var message = $"There was an error starting the browser. Please visit \"{e.Uri.AbsoluteUri}\" manually to create an issue.";
			    ShowExceptionMessageBox(exception, "Error starting browser", message);
			}
		}

		private void CloseDoNotMarkHandled_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;

			if (this.CheckBoxRememberIsChecked())
			{
				SnoopModes.IgnoreExceptions = true;
			}

			this.Close();
		}

		private void CloseAndMarkHandled_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;

			if (this.CheckBoxRememberIsChecked())
			{
				SnoopModes.SwallowExceptions = true;
			}

			this.Close();
		}

		private string GetExceptionMessage()
		{
		    return this.Exception.ToString();
		}

		private bool CheckBoxRememberIsChecked()
		{
			return this._checkBoxRemember.IsChecked.HasValue && this._checkBoxRemember.IsChecked.Value;
		}

	    private string GenerateIssueBody()
	    {
	        return $@"**Place your issue description here.**

Exception details:
{this.Exception}

---
### Environment
- Snoop {this.GetType().Assembly.GetName().Version}
- Windows {GetWindowsVersion()}
- .NET Framework {Environment.Version}";
	    }

	    private static string GetWindowsVersion()
	    {
	        try
	        {
	            using (var registryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
	            {
	                return (string)registryKey.GetValue("CurrentVersion") + "." + (string)registryKey.GetValue("CurrentBuild");
	            }
	        }
	        catch (Exception)
	        {
	            return Environment.OSVersion.Version + " " + Environment.OSVersion.ServicePack;
	        }	        
	    }
	}
}