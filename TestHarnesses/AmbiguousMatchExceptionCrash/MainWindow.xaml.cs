namespace AmbiguousMatchExceptionCrash
{
    using System.Windows.Controls;

    public partial class MainWindow
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
            get => base.Width;
            set => base.Width = value.GetValueOrDefault();
        }
    }
}