// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Snoop.Windows;

public partial class Previewer
{
    public static readonly RoutedCommand MagnifyCommand = new(nameof(MagnifyCommand), typeof(Previewer));
    public static readonly RoutedCommand ScreenshotCommand = new(nameof(ScreenshotCommand), typeof(Previewer));

    public Previewer()
    {
        this.InitializeComponent();

        this.CommandBindings.Add(new CommandBinding(MagnifyCommand, this.HandleMagnify, this.HandleCanMagnify));
        this.CommandBindings.Add(new CommandBinding(ScreenshotCommand, this.HandleScreenshot, this.HandleCanScreenshot));
    }

    #region Target

    /// <summary>
    /// Gets or sets the Target property.
    /// </summary>
    public object Target
    {
        get { return (object)this.GetValue(TargetProperty); }
        set { this.SetValue(TargetProperty, value); }
    }

    /// <summary>
    /// Target Dependency Property
    /// </summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(object),
            typeof(Previewer),
            new FrameworkPropertyMetadata(
                default,
                OnTargetChanged));

    /// <summary>
    /// Handles changes to the Target property.
    /// </summary>
    private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Previewer)d).OnTargetChanged(e);
    }

    /// <summary>
    /// Provides derived classes an opportunity to handle changes to the Target property.
    /// </summary>
    protected virtual void OnTargetChanged(DependencyPropertyChangedEventArgs e)
    {
        this.HandleTargetOrIsActiveChanged();
    }
    #endregion

    #region IsActive

    /// <summary>
    /// Gets or sets the IsActive property.
    /// </summary>
    public bool IsActive
    {
        get { return (bool)this.GetValue(IsActiveProperty); }
        set { this.SetValue(IsActiveProperty, value); }
    }

    /// <summary>
    /// IsActive Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(Previewer),
            new FrameworkPropertyMetadata(
                (bool)true,
                OnIsActiveChanged));

    /// <summary>
    /// Handles changes to the IsActive property.
    /// </summary>
    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Previewer)d).OnIsActiveChanged(e);
    }

    /// <summary>
    /// Provides derived classes an opportunity to handle changes to the IsActive property.
    /// </summary>
    protected virtual void OnIsActiveChanged(DependencyPropertyChangedEventArgs e)
    {
        this.HandleTargetOrIsActiveChanged();
    }

    #endregion

    private void HandleTargetOrIsActiveChanged()
    {
        if (this.IsActive && this.Target is Visual)
        {
            var visual = (Visual)this.Target;
            this.Zoomer.Target = visual;
        }
        else
        {
            var pooSniffer = (Brush)this.FindResource("previewerSnoopDogDrawingBrush");
            this.Zoomer.Target = pooSniffer;
        }
    }

    private void HandleCanMagnify(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = this.Target as Visual is not null;
        e.Handled = true;
    }

    private void HandleMagnify(object sender, ExecutedRoutedEventArgs e)
    {
        var zoomer = new Zoomer
        {
            ColorSlider =
            {
                Value = this.Zoomer.ColorSlider.Value
            }
        };
        zoomer.Inspect(this.Target);
        e.Handled = true;
    }

    private void HandleCanScreenshot(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = this.Target as Visual is not null;
        e.Handled = true;
    }

    private void HandleScreenshot(object sender, ExecutedRoutedEventArgs e)
    {
        var visual = this.Target as Visual;

        var dialog = new ScreenshotDialog
        {
            DataContext = visual
        };

        dialog.ShowDialogEx(this);

        e.Handled = true;
    }
}