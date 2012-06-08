using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace NoMainWindowForApp
{
	class NoMainWindowForApp
	{
		[STAThread]
		public static void Main()
		{
			// original (doesn't work with snoop, well, can't find a window to own the snoop ui)
			Window window = new Window();
			window.Title = "Say Hello";
			window.Show();

			Application application = new Application();
			application.Run();


			// setting the MainWindow directly (works with snoop)
//			Window window = new Window();
//			window.Title = "Say Hello";
//			window.Show();
//
//			Application application = new Application();
//			application.MainWindow = window;
//			application.Run();


			// creating the application first, then the window (works with snoop)
//			Application application = new Application();
//			Window window = new Window();
//			window.Title = "Say Hello";
//			window.Show();
//			application.Run();


			// creating the application first, then the window (works with snoop)
//			Application application = new Application();
//			Window window = new Window();
//			window.Title = "Say Hello";
//			application.Run(window);
		}	}
}
