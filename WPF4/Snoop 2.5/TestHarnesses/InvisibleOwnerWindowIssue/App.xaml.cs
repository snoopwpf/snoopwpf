using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace InvisibleOwnerWindowIssue
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private Window _hiddenWindow;
		private Window _shownWindow;

		protected override void OnStartup(StartupEventArgs args)
		{
			base.OnStartup(args);

			_hiddenWindow = new Window();
			_hiddenWindow.Title = "Hidden";

			_shownWindow = new Window();
			_shownWindow.Title = "Shown";
			_shownWindow.Closing += (sender, e) =>
			{
				_hiddenWindow.Close();
			};
			_shownWindow.Show();
		}
	}
}
