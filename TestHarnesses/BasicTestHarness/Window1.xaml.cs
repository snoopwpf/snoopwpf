﻿namespace BasicTestHarness
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
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
            var binding = new Binding("DoesNotExist");
            PresentationTraceSources.SetTraceLevel(binding, PresentationTraceLevel.High);
            this.textBlockForBindingError.SetBinding(TextBlock.TextProperty, binding);
        }
    }
}
