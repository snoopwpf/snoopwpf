namespace Snoop.Views;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Snoop.Infrastructure;
using Snoop.Infrastructure.SelectionHighlight;

public partial class HighlightSettingsView
{
    private static readonly HashSet<string> realSettingsProperties = new()
    {
        nameof(SelectionHighlightOptions.Default.Background),
        nameof(SelectionHighlightOptions.Default.BorderBrush),
        nameof(SelectionHighlightOptions.Default.BorderThickness),
    };

    public static readonly DependencyProperty PropertiesProperty = DependencyProperty.Register(nameof(Properties), typeof(ObservableCollection<PropertyInformation>), typeof(HighlightSettingsView), new PropertyMetadata(default(ObservableCollection<PropertyInformation>)));

    public HighlightSettingsView()
    {
        this.InitializeComponent();

        this.Properties = new ObservableCollection<PropertyInformation>(TypeDescriptor.GetProperties(SelectionHighlightOptions.Default)
            .OfType<PropertyDescriptor>()
            .Where(x => realSettingsProperties.Contains(x.Name))
            .Select(x => new PropertyInformation(SelectionHighlightOptions.Default, x, x.Name, x.DisplayName)));
    }

    public ObservableCollection<PropertyInformation>? Properties
    {
        get => (ObservableCollection<PropertyInformation>?)this.GetValue(PropertiesProperty);
        set => this.SetValue(PropertiesProperty, value);
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)?.Close();
    }

    private void ResetSettings_OnClick(object sender, RoutedEventArgs e)
    {
        SelectionHighlightOptions.Default.Reset();
    }
}