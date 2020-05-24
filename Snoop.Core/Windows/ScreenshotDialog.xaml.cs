// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Windows
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Win32;
    using Snoop.Infrastructure;

    /// <summary>
    /// Interaction logic for ScreenShotDialog.xaml
    /// </summary>
    public partial class ScreenshotDialog
    {
        public static readonly RoutedCommand SaveCommand = new RoutedCommand(nameof(SaveCommand), typeof(ScreenshotDialog));
        public static readonly RoutedCommand CancelCommand = new RoutedCommand(nameof(CancelCommand), typeof(ScreenshotDialog));

        public ScreenshotDialog()
        {
            this.InitializeComponent();

            this.CommandBindings.Add(new CommandBinding(SaveCommand, this.HandleSave, this.HandleCanSave));
            this.CommandBindings.Add(new CommandBinding(CancelCommand, this.HandleCancel, (x, y) => y.CanExecute = true));
        }

        #region FilePath Dependency Property

        public string FilePath
        {
            get { return (string)this.GetValue(FilePathProperty); }
            set { this.SetValue(FilePathProperty, value); }
        }

        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(
                nameof(FilePath),
                typeof(string),
                typeof(ScreenshotDialog),
                new PropertyMetadata());

        #endregion

        private void HandleCanSave(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.DataContext is Visual;
        }

        private void HandleSave(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.FilePath))
            {
                var filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                var filename = "SnoopScreenshot.png";

                if (this.DataContext is FrameworkElement element
                    && string.IsNullOrEmpty(element.Name) == false)
                {
                    filename = $"SnoopScreenshot_{element.Name}.png";
                }

                this.FilePath = Path.Combine(filePath, filename);
            }

            var fileDialog = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = "png",
                InitialDirectory = Path.GetDirectoryName(this.FilePath),
                FileName = Path.GetFileNameWithoutExtension(this.FilePath),
                Filter = "Image File (*.png)|*.png",
                FilterIndex = 0
            };

            if (fileDialog.ShowDialog(this).Value)
            {
                this.FilePath = fileDialog.FileName;

                VisualCaptureUtil.SaveVisual(this.DataContext as Visual,
                    int.Parse(((TextBlock)((ComboBoxItem)this.dpiBox.SelectedItem).Content).Text),
                    this.FilePath);

                this.Close();
            }
        }

        private void HandleCancel(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }
}
