namespace AmbiguousMatchExceptionCrash
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }
    }

    public class MyButton : Button
    {
        public new double? Width
        {
            get { return base.Width; }
            set { base.Width = value.GetValueOrDefault(); }
        }
    }
}
