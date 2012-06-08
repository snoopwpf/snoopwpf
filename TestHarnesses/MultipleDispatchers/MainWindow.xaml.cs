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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;

namespace MultipleDispatchers
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private static void NewWindow(bool newDispatcher)
		{
			if (newDispatcher)
			{
				Thread thread = new Thread(new ThreadStart(NewDispatcherWindow));
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
			}
			else
			{
				var mainWindow = new MainWindow();
				mainWindow.Show();
			}
		}

		private static void NewDispatcherWindow()
		{
			MainWindow newWindow = new MainWindow();
			newWindow.Show();
			Dispatcher.Run();
		}

		private void dispatcherLaunchButton_Click(object sender, RoutedEventArgs e)
		{
			NewWindow(true);
		}

		private void noDispatcherLaunchButton_Click(object sender, RoutedEventArgs e)
		{
			NewWindow(false);
		}
	}
}
