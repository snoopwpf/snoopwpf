namespace WhiteForegroundIssue
{
    using System;
    using System.Windows;

    public partial class MainWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            throw new Exception("This is a test exception.");
        }
    }
}
