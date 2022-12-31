// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Windows;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Snoop.Controls;
using Snoop.Core;
using Snoop.Infrastructure;

public sealed partial class Zoomer
{
    private readonly TranslateTransform translation = new();
    private readonly ScaleTransform zoom = new();
    private readonly TransformGroup transform = new();
    private Point downPoint;
    private object? target;
    private Visual? targetVisual;
    private VisualTree3DView? visualTree3DView;

    private const double ZoomFactor = 1.1;

    static Zoomer()
    {
        ResetCommand = new RoutedCommand("Reset", typeof(Zoomer));
        ZoomInCommand = new RoutedCommand("ZoomIn", typeof(Zoomer));
        ZoomOutCommand = new RoutedCommand("ZoomOut", typeof(Zoomer));
        PanLeftCommand = new RoutedCommand("PanLeft", typeof(Zoomer));
        PanRightCommand = new RoutedCommand("PanRight", typeof(Zoomer));
        PanUpCommand = new RoutedCommand("PanUp", typeof(Zoomer));
        PanDownCommand = new RoutedCommand("PanDown", typeof(Zoomer));
        SwitchTo2DCommand = new RoutedCommand("SwitchTo2D", typeof(Zoomer));
        SwitchTo3DCommand = new RoutedCommand("SwitchTo3D", typeof(Zoomer));

        ResetCommand.InputGestures.Add(new MouseGesture(MouseAction.LeftDoubleClick));
        ResetCommand.InputGestures.Add(new KeyGesture(Key.F5));
        ZoomInCommand.InputGestures.Add(new KeyGesture(Key.OemPlus));
        ZoomInCommand.InputGestures.Add(new KeyGesture(Key.Up, ModifierKeys.Control));
        ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.OemMinus));
        ZoomOutCommand.InputGestures.Add(new KeyGesture(Key.Down, ModifierKeys.Control));
        PanLeftCommand.InputGestures.Add(new KeyGesture(Key.Left));
        PanRightCommand.InputGestures.Add(new KeyGesture(Key.Right));
        PanUpCommand.InputGestures.Add(new KeyGesture(Key.Up));
        PanDownCommand.InputGestures.Add(new KeyGesture(Key.Down));
        SwitchTo2DCommand.InputGestures.Add(new KeyGesture(Key.F2));
        SwitchTo3DCommand.InputGestures.Add(new KeyGesture(Key.F3));
    }

    public Zoomer()
    {
        this.CommandBindings.Add(new CommandBinding(ResetCommand, this.HandleReset, this.CanReset));
        this.CommandBindings.Add(new CommandBinding(ZoomInCommand, this.HandleZoomIn));
        this.CommandBindings.Add(new CommandBinding(ZoomOutCommand, this.HandleZoomOut));
        this.CommandBindings.Add(new CommandBinding(PanLeftCommand, this.HandlePanLeft));
        this.CommandBindings.Add(new CommandBinding(PanRightCommand, this.HandlePanRight));
        this.CommandBindings.Add(new CommandBinding(PanUpCommand, this.HandlePanUp));
        this.CommandBindings.Add(new CommandBinding(PanDownCommand, this.HandlePanDown));
        this.CommandBindings.Add(new CommandBinding(SwitchTo2DCommand, this.HandleSwitchTo2D));
        this.CommandBindings.Add(new CommandBinding(SwitchTo3DCommand, this.HandleSwitchTo3D, this.CanSwitchTo3D));

        this.InitializeComponent();

        this.transform.Children.Add(this.zoom);
        this.transform.Children.Add(this.translation);

        this.Viewbox.RenderTransform = this.transform;
    }

    protected override void Load(object root)
    {
        if (root is Application application
            && application.CheckAccess())
        {
            // try to use the application's main window (if visible) as the root
            if (application.MainWindow is not null
                && application.MainWindow.Visibility == Visibility.Visible)
            {
                root = application.MainWindow;
            }
            else
            {
                // else search for the first visible window in the list of the application's windows
                foreach (Window? appWindow in application.Windows)
                {
                    if (appWindow is null)
                    {
                        continue;
                    }

                    if (appWindow.CheckAccess()
                        && appWindow.Visibility == Visibility.Visible)
                    {
                        root = appWindow;
                        break;
                    }
                }
            }
        }

        this.Target = root;
    }

    public override object? Target
    {
        get => this.target;

        set
        {
            this.target = value;
            this.targetVisual = value as Visual;
            var element = this.CreateIfPossible(value);
            this.Viewbox.Child = element;
        }
    }

    public static readonly RoutedCommand ResetCommand;
    public static readonly RoutedCommand ZoomInCommand;
    public static readonly RoutedCommand ZoomOutCommand;
    public static readonly RoutedCommand PanLeftCommand;
    public static readonly RoutedCommand PanRightCommand;
    public static readonly RoutedCommand PanUpCommand;
    public static readonly RoutedCommand PanDownCommand;
    public static readonly RoutedCommand SwitchTo2DCommand;
    public static readonly RoutedCommand SwitchTo3DCommand;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // load the window placement details from the user settings.
        SnoopWindowUtils.LoadWindowPlacement(this, Settings.Default.ZoomerWindowPlacement);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // persist the window placement details to the user settings.
        SnoopWindowUtils.SaveWindowPlacement(this, wp => Settings.Default.ZoomerWindowPlacement = wp);

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        this.Viewbox.Child = null;

        base.OnClosed(e);
    }

    private void HandleReset(object sender, ExecutedRoutedEventArgs args)
    {
        this.translation.X = 0;
        this.translation.Y = 0;
        this.zoom.ScaleX = 1;
        this.zoom.ScaleY = 1;
        this.zoom.CenterX = 0;
        this.zoom.CenterY = 0;

        if (this.visualTree3DView is not null)
        {
            this.visualTree3DView = null;
            this.ZScaleSlider.Value = 0;
            this.dpiBox.SelectedIndex = 2;

            this.CreateAndSetVisualTree3DView(this.targetVisual);
        }
    }

    private void CanReset(object sender, CanExecuteRoutedEventArgs args)
    {
        args.CanExecute = true;
        args.Handled = true;
    }

    private void HandleZoomIn(object sender, ExecutedRoutedEventArgs args)
    {
        var offset = Mouse.GetPosition(this.Viewbox);
        this.Zoom(ZoomFactor, offset);
    }

    private void HandleZoomOut(object sender, ExecutedRoutedEventArgs args)
    {
        var offset = Mouse.GetPosition(this.Viewbox);
        this.Zoom(1 / ZoomFactor, offset);
    }

    private void HandlePanLeft(object sender, ExecutedRoutedEventArgs args)
    {
        this.translation.X -= 5;
    }

    private void HandlePanRight(object sender, ExecutedRoutedEventArgs args)
    {
        this.translation.X += 5;
    }

    private void HandlePanUp(object sender, ExecutedRoutedEventArgs args)
    {
        this.translation.Y -= 5;
    }

    private void HandlePanDown(object sender, ExecutedRoutedEventArgs args)
    {
        this.translation.Y += 5;
    }

    private void HandleSwitchTo2D(object sender, ExecutedRoutedEventArgs args)
    {
        if (this.visualTree3DView is not null)
        {
            this.Target = this.target;
            this.visualTree3DView = null;
            this.ThreeDViewControls.Visibility = Visibility.Collapsed;
        }
    }

    private void HandleSwitchTo3D(object sender, ExecutedRoutedEventArgs args)
    {
        if (this.visualTree3DView is null
            && this.targetVisual is not null)
        {
            this.CreateAndSetVisualTree3DView(this.targetVisual);

            this.ThreeDViewControls.Visibility = Visibility.Visible;
        }
    }

    private void CreateAndSetVisualTree3DView(Visual? visual)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (visual is not null)
            {
                this.visualTree3DView = new VisualTree3DView(visual, int.Parse(((TextBlock)((ComboBoxItem)this.dpiBox.SelectedItem).Content).Text));
            }
            else
            {
                this.visualTree3DView = null;
            }

            this.Viewbox.Child = this.visualTree3DView;
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void CanSwitchTo3D(object sender, CanExecuteRoutedEventArgs args)
    {
        args.CanExecute = this.targetVisual is not null;
        args.Handled = true;
    }

    private void Content_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.downPoint = e.GetPosition(this.DocumentRoot);
        this.DocumentRoot.CaptureMouse();
    }

    private void Content_MouseMove(object sender, MouseEventArgs e)
    {
        if (this.DocumentRoot.IsMouseCaptured)
        {
            var delta = e.GetPosition(this.DocumentRoot) - this.downPoint;
            this.translation.X += delta.X;
            this.translation.Y += delta.Y;

            this.downPoint = e.GetPosition(this.DocumentRoot);
        }
    }

    private void Content_MouseUp(object sender, MouseEventArgs e)
    {
        this.DocumentRoot.ReleaseMouseCapture();
    }

    private void Content_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var newZoom = Math.Pow(ZoomFactor, e.Delta / 120.0);
        var offset = e.GetPosition(this.Viewbox);
        this.Zoom(newZoom, offset);
    }

    private void ZScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (this.visualTree3DView is not null)
        {
            this.visualTree3DView.ZScale = Math.Pow(10, e.NewValue);
        }
    }

    private UIElement? CreateIfPossible(object? item)
    {
        return ZoomerUtilities.CreateIfPossible(item);
    }

    private void Zoom(double newZoom, Point offset)
    {
        var v = new Vector((1 - newZoom) * offset.X, (1 - newZoom) * offset.Y);

        var translationVector = v * this.transform.Value;
        this.translation.X += translationVector.X;
        this.translation.Y += translationVector.Y;

        this.zoom.ScaleX *= newZoom;
        this.zoom.ScaleY *= newZoom;
    }

    private void DpiBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.visualTree3DView is not null
            && this.targetVisual is not null)
        {
            this.translation.X = 0;
            this.translation.Y = 0;
            this.zoom.ScaleX = 1;
            this.zoom.ScaleY = 1;
            this.zoom.CenterX = 0;
            this.zoom.CenterY = 0;

            this.CreateAndSetVisualTree3DView(this.targetVisual);
        }
    }

    private void FixTextFormattingModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (this.targetVisual is not null)
        {
            TextOptions.SetTextFormattingMode(this.targetVisual, TextFormattingMode.Ideal);
        }
    }
}

[ValueConversion(typeof(double), typeof(SolidColorBrush))]
[ValueConversion(typeof(float), typeof(SolidColorBrush))]
public sealed class DoubleToWhitenessConverter : IValueConverter
{
    public static readonly DoubleToWhitenessConverter Default = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var val = (float)(double)value;
        var c = new Color
        {
            ScR = val,
            ScG = val,
            ScB = val,
            ScA = 1
        };

        return new SolidColorBrush(c);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}