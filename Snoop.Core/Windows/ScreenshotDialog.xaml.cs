// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Windows;

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
    private static string lastSaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    public static readonly RoutedCommand SaveCommand = new(nameof(SaveCommand), typeof(ScreenshotDialog));
    public static readonly RoutedCommand CancelCommand = new(nameof(CancelCommand), typeof(ScreenshotDialog));

    public ScreenshotDialog()
    {
        this.InitializeComponent();

        this.CommandBindings.Add(new CommandBinding(SaveCommand, this.HandleSave, this.HandleCanSave));
        this.CommandBindings.Add(new CommandBinding(CancelCommand, this.HandleCancel, (_, y) => y.CanExecute = true));
    }

    private void HandleCanSave(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = this.DataContext is Visual;
    }

    private void HandleSave(object sender, ExecutedRoutedEventArgs e)
    {
        var dpiText = ((TextBlock)((ComboBoxItem)this.dpiBox.SelectedItem).Content).Text;

        var filename = "SnoopScreenshot";

        if (this.DataContext is FrameworkElement element
            && string.IsNullOrEmpty(element.Name) == false)
        {
            filename = $"SnoopScreenshot_{element.Name}";
        }

        filename += "_" + dpiText;

        filename += ".png";

        var filePath = Path.Combine(lastSaveDirectory, filename);

        var fileDialog = new SaveFileDialog
        {
            AddExtension = true,
            CheckPathExists = true,
            DefaultExt = "png",
            InitialDirectory = Path.GetDirectoryName(filePath),
            FileName = Path.GetFileNameWithoutExtension(filePath),
            Filter = "Image File (*.png)|*.png",
            FilterIndex = 0
        };

        if (fileDialog.ShowDialog(this) == true)
        {
            filePath = fileDialog.FileName;

            var directoryName = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directoryName) == false)
            {
                lastSaveDirectory = directoryName;
            }

            VisualCaptureUtil.SaveVisual(this.DataContext as Visual, int.Parse(dpiText), filePath);

            this.Close();
        }
    }

    private void HandleCancel(object sender, ExecutedRoutedEventArgs e)
    {
        this.Close();
    }
}