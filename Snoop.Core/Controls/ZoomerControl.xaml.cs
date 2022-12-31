// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls;

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Snoop.Infrastructure;

public partial class ZoomerControl
{
    public ZoomerControl()
    {
        this.InitializeComponent();

        this.transform.Children.Add(this.zoom);
        this.transform.Children.Add(this.translation);

        this.Viewbox.RenderTransform = this.transform;
    }

    #region Target

    /// <summary>
    /// Gets or sets the Target property.
    /// </summary>
    public object? Target
    {
        get { return (object?)this.GetValue(TargetProperty); }
        set { this.SetValue(TargetProperty, value); }
    }

    /// <summary>
    /// Target Dependency Property
    /// </summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(object),
            typeof(ZoomerControl),
            new FrameworkPropertyMetadata(
                null,
                OnTargetChanged));

    /// <summary>
    /// Handles changes to the Target property.
    /// </summary>
    private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ZoomerControl)d).OnTargetChanged(e);
    }

    /// <summary>
    /// Provides derived classes an opportunity to handle changes to the Target property.
    /// </summary>
    protected virtual void OnTargetChanged(DependencyPropertyChangedEventArgs e)
    {
        this.ResetZoomAndTranslation();

        if (this.pooSniffer is null)
        {
            this.pooSniffer = this.TryFindResource("poo_sniffer_xpr") as Brush;
        }

        this.Cursor = ReferenceEquals(this.Target, this.pooSniffer) ? null : Cursors.SizeAll;

        var element = this.CreateIfPossible(this.Target);
        if (element is not null)
        {
            this.Viewbox.Child = element;
        }
    }
    #endregion

    private void Content_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.Focus();
        this.downPoint = e.GetPosition(this.DocumentRoot);
        this.DocumentRoot.CaptureMouse();
    }

    private void Content_MouseMove(object sender, MouseEventArgs e)
    {
        if (this.IsValidTarget
            && this.DocumentRoot.IsMouseCaptured)
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

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (this.IsValidTarget)
        {
            e.Handled = true;

            var zoom = Math.Pow(ZoomFactor, e.Delta / 120.0);
            var offset = e.GetPosition(this.Viewbox);

            this.Zoom(zoom, offset);
        }
    }

    private void ResetZoomAndTranslation()
    {
        //Zoom(0, new Point(-this.translation.X, -this.translation.Y));
        //Zoom(1.0 / zoom.ScaleX, new Point());
        this.zoom.ScaleX = 1.0;
        this.zoom.ScaleY = 1.0;

        this.translation.X = 0.0;
        this.translation.Y = 0.0;
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

    private bool IsValidTarget => this.Target is not null
                                  && ReferenceEquals(this.Target, this.pooSniffer) == false;

    private Brush? pooSniffer;

    private readonly TranslateTransform translation = new();
    private readonly ScaleTransform zoom = new();
    private readonly TransformGroup transform = new();
    private Point downPoint;

    private const double ZoomFactor = 1.1;
}