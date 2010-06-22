// Copyright © 2006 Microsoft Corporation.  All Rights Reserved

namespace Snoop {
	
	using System;
	using System.IO;
	using System.Diagnostics;
	using System.Reflection;
	using System.Windows;
	using System.Threading;

	/// <summary>
	/// Main app entry.
	/// </summary>
	public static class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			Application app = new Application();
			app.MainWindow = new AppChooser();
			app.MainWindow.Show();
			return app.Run();
		}
	}
}