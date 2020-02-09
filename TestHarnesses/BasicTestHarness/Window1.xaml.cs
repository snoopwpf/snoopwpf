namespace BasicTestHarness
{
    using System;
    using System.Windows;

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            throw new Exception("This is a test exception.");
        }
    }
}
