using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

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
			_shownWindow.Width = 300;
			_shownWindow.Height = 300;
			_shownWindow.Title = "Shown";
			Rectangle rectangle = new Rectangle();
			rectangle.Width = 200;
			rectangle.Height = 50;
			rectangle.HorizontalAlignment = HorizontalAlignment.Center;
			rectangle.VerticalAlignment = VerticalAlignment.Center;
			rectangle.Fill = new SolidColorBrush(Colors.DarkRed);
			_shownWindow.Content = rectangle;
			_shownWindow.Closing += (sender, e) =>
			{
				_hiddenWindow.Close();
			};
			_shownWindow.Show();
		}
	}
}
