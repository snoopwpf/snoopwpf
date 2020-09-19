namespace MultipleDispatchers
{
    using System.Threading;
    using System.Windows;
    using System.Windows.Threading;

    public partial class MainWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private static void NewWindow(bool newDispatcher)
        {
            if (newDispatcher)
            {
                var thread = new Thread(NewDispatcherWindow)
                {
                    IsBackground = true
                };
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
            var newWindow = new MainWindow();
            newWindow.Show();
            Dispatcher.Run();
        }

        private void DispatcherLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            NewWindow(true);
        }

        private void NoDispatcherLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            NewWindow(false);
        }
    }
}