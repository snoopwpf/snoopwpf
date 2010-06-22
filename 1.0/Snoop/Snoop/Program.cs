namespace Snoop {
	
	using System;
	using System.IO;
	using System.Diagnostics;
	using System.Reflection;
	using System.Windows.Forms;
	using System.Threading;

	/// <summary>
	/// Main app entry.
	/// </summary>
	public static class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			AppChooser appChooser = new AppChooser();
			bool? result = appChooser.ShowDialog();
			if (result == null || result.Value == false)
				return -1;
			return 0;
		}
	}
}