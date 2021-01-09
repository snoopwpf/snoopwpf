namespace BasicTestHarness
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    public partial class Window1
    {
        public Window1()
        {
            this.InitializeComponent();
        }

        private void ExceptionButton_Click(object sender, RoutedEventArgs e)
        {
            throw new Exception("This is a test exception.");
        }

        private void BindingErrorButton_Click(object sender, RoutedEventArgs e)
        {
            var binding = new Binding("DoesNotExistInCode");
            BindingOperations.SetBinding(this.textBlockForBindingError, TextBlock.TextProperty, binding);
        }
    }
}
