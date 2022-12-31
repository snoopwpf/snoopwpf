// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop;

using System;
using System.Windows;
using Snoop.Windows;

public partial class App
{
    public App()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Snoop.Core;component/Icons.xaml") });

        //this.RunInDispatcherAsync(this.SnoopSelf, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void SnoopSelf()
    {
        var ui = new SnoopUI().Inspect(this.MainWindow);
        ui.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        Settings.Default.Save();
    }
}