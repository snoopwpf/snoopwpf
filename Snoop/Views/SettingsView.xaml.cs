namespace Snoop.Views;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Snoop.Infrastructure;

public partial class SettingsView
{
    private static readonly HashSet<string> realSettingsProperties = new()
    {
        nameof(Settings.Default.MultipleAppDomainMode),
        nameof(Settings.Default.MultipleDispatcherMode),
        nameof(Settings.Default.SetOwnerWindow),
        nameof(Settings.Default.EnableDiagnostics),
        nameof(Settings.Default.GlobalHotKey),
        nameof(Settings.Default.ILSpyPath),
    };

    public static readonly DependencyProperty PropertiesProperty = DependencyProperty.Register(nameof(Properties), typeof(ObservableCollection<PropertyInformation>), typeof(SettingsView), new PropertyMetadata(default(ObservableCollection<PropertyInformation>)));

    public SettingsView()
    {
        this.InitializeComponent();

        this.Properties = new ObservableCollection<PropertyInformation>(TypeDescriptor.GetProperties(Settings.Default)
            .OfType<PropertyDescriptor>()
            .Where(x => realSettingsProperties.Contains(x.Name))
            .Select(x => new PropertyInformation(Settings.Default, x, x.Name, x.DisplayName)));
    }

    public ObservableCollection<PropertyInformation>? Properties
    {
        get => (ObservableCollection<PropertyInformation>?)this.GetValue(PropertiesProperty);
        set => this.SetValue(PropertiesProperty, value);
    }

    private void SaveSettingsAndClose_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Default.Save();

        if (Window.GetWindow(this) is { } ownerWindow)
        {
            ownerWindow.DialogResult = true;
            ownerWindow.Close();
        }
    }

    private void DiscardSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Default.Reload();
    }

    private void ResetAllSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Default.Reset();
    }
}