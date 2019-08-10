// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Windows;
    using Snoop.Properties;

    public partial class App
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if NET40
            const string assemblyFramework = "net40";
#elif NETCOREAPP30
            const string assemblyFramework = "netcoreapp3.0";
#else
            // generate invalid code to force a compiler error
            asdf ölkj
#endif

            this.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Snoop.Core.{assemblyFramework};component/Icons.xaml") });
            this.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Snoop.Core.{assemblyFramework};component/ValueEditors/EditorTemplates.xaml") });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}