namespace InvisibleOwnerWindowIssue
{
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Window hiddenWindow;
        private Window shownWindow;

        protected override void OnStartup(StartupEventArgs args)
        {
            base.OnStartup(args);

            this.hiddenWindow = new Window();
            this.hiddenWindow.Title = "Hidden";

            this.shownWindow = new Window();
            this.shownWindow.Width = 300;
            this.shownWindow.Height = 300;
            this.shownWindow.Title = "Shown";
            Rectangle rectangle = new Rectangle();
            rectangle.Width = 200;
            rectangle.Height = 50;
            rectangle.HorizontalAlignment = HorizontalAlignment.Center;
            rectangle.VerticalAlignment = VerticalAlignment.Center;
            rectangle.Fill = new SolidColorBrush(Colors.DarkRed);
            this.shownWindow.Content = rectangle;
            this.shownWindow.Closing += (sender, e) =>
            {
                this.hiddenWindow.Close();
            };
            this.shownWindow.Show();
        }
    }
}
