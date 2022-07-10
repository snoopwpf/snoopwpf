// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System.Windows;

/// <summary>
/// Interaction logic for ParameterCreator.xaml
/// </summary>
public partial class ParameterCreator
{
    public ParameterCreator()
    {
        this.InitializeComponent();
    }

    public object? RootTarget
    {
        get { return this.GetValue(RootTargetProperty); }
        set { this.SetValue(RootTargetProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RootTargetProperty =
        DependencyProperty.Register(nameof(RootTarget), typeof(object), typeof(ParameterCreator), new UIPropertyMetadata(null));

    public object? SelectedTarget
    {
        get;
        private set;
    }

    private void OKClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.SelectedTarget = this.propertyInspector.Target;
        this.Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }
}