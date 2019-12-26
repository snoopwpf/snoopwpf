// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.PowerShell
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using Snoop.Infrastructure;

    public partial class EmbeddedShellView
    {
        public event Action<VisualTreeItem> ProviderLocationChanged = delegate { };

        private Runspace runspace;
        private SnoopPSHost host;
        private int historyIndex;

        private bool isStarted;

        public EmbeddedShellView()
        {
            this.InitializeComponent();

            this.commandTextBox.PreviewKeyDown += this.OnCommandTextBoxPreviewKeyDown;
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(EmbeddedShellView), new PropertyMetadata(default(bool), OnIsSelectedChanged));
        private SnoopUI snoopUi;
        private bool isSettingLocationFromLocationProvider;

        public bool IsSelected
        {
            get { return (bool)this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (EmbeddedShellView)d;

            if ((bool)e.NewValue == false)
            {
                return;
            }

            view.WhenLoaded(x => ((EmbeddedShellView)x).Start(VisualTreeHelper2.GetAncestor<SnoopUI>((EmbeddedShellView)x)));
        }

        /// <summary>
        /// Initiates the startup routine and configures the runspace for use.
        /// </summary>
        private void Start(SnoopUI snoopUi)
        {
            if (this.isStarted)
            {
                return;
            }

            if (ShellConstants.IsPowerShellInstalled == false)
            {
                return;
            }

            this.isStarted = true;

            this.snoopUi = snoopUi;

            {
                // ignore execution-policy
                var iis = InitialSessionState.CreateDefault();
                iis.AuthorizationManager = new AuthorizationManager(Guid.NewGuid().ToString());
                iis.Providers.Add(new SessionStateProviderEntry(ShellConstants.DriveName, typeof(VisualTreeProvider), string.Empty));

                this.host = new SnoopPSHost(this.outputTextBox, x => this.outputTextBox.AppendText(x));
                this.runspace = RunspaceFactory.CreateRunspace(this.host, iis);
                this.runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;

#if NET40
                this.runspace.ApartmentState = System.Threading.ApartmentState.STA;
#endif

                this.runspace.Open();
            }

            {
                snoopUi.PropertyChanged += this.OnSnoopUiOnPropertyChanged;

                // allow scripting of the host controls
                this.SetVariable("snoopui", snoopUi);
                this.SetVariable("ui", this);
                this.SetVariable(ShellConstants.Root, snoopUi.Root);
                this.SetVariable(ShellConstants.Selected, snoopUi.CurrentSelection);

                // marshall back to the UI thread when the provider notifiers of a location change
                var action = new Action<VisualTreeItem>(item => this.Dispatcher.BeginInvoke(new Action(() => this.ProviderLocationChanged(item))));
                this.SetVariable(ShellConstants.LocationChangedActionKey, action);

                var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "Scripts");
                this.Invoke($"import-module \"{Path.Combine(folder, ShellConstants.SnoopModule)}\"");

                this.outputTextBox.Clear();

                this.Invoke("write-host 'Welcome to the Snoop PowerShell console!'");
                this.Invoke("write-host '----------------------------------------'");
                this.Invoke($"write-host 'To get started, try using the ${ShellConstants.Root} and ${ShellConstants.Selected} variables.'");

                this.FindAndLoadProfile(folder);

                this.NotifySelected(snoopUi.CurrentSelection);
            }

            {

                // sync the current location
                snoopUi.Tree.SelectedItemChanged += this.OnSnoopUiSelectedItemChanged;
                this.ProviderLocationChanged += this.OnProviderLocationChanged;

                // clean up garbage!
                snoopUi.Closed += this.OnSnoopUiClosed;
            }
        }

        private void Shutdown()
        {
            this.UnsubscribeSnoopUiEvents();

            this.ProviderLocationChanged -= this.OnProviderLocationChanged;

            this.host = null;

            this.runspace?.Dispose();
            this.runspace = null;

            this.isStarted = false;
        }

        private void UnsubscribeSnoopUiEvents()
        {
            if (this.snoopUi != null)
            {
                this.snoopUi.PropertyChanged -= this.OnSnoopUiOnPropertyChanged;
                this.snoopUi.Tree.SelectedItemChanged -= this.OnSnoopUiSelectedItemChanged;
                this.snoopUi.Closed -= this.OnSnoopUiClosed;
            }
        }

        private void OnSnoopUiClosed(object sender, EventArgs e)
        {
            this.Shutdown();
        }

        private void OnSnoopUiOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var snoopUi = (SnoopUI)sender;

            switch (e.PropertyName)
            {
                case nameof(SnoopUI.CurrentSelection):
                    this.SetVariable(ShellConstants.Selected, snoopUi.CurrentSelection);
                    break;

                case nameof(SnoopUI.Root):
                    this.SetVariable(ShellConstants.Root, snoopUi.Root);
                    break;
            }
        }

        private void OnSnoopUiSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) => this.NotifySelected(this.snoopUi.CurrentSelection);

        private void OnProviderLocationChanged(VisualTreeItem item) =>
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.isSettingLocationFromLocationProvider = true;
                try
                {
                    item.IsSelected = true;
                    this.snoopUi.CurrentSelection = item;
                }
                finally
                {
                    this.isSettingLocationFromLocationProvider = false;
                }
            }));

        public void SetVariable(string name, object instance)
        {
            // add to the host so the provider has access to exposed variables
            this.host[name] = instance;

            // expose to the current runspace
            this.Invoke($"${name} = $host.PrivateData['{name}']");
        }

        public void NotifySelected(VisualTreeItem item)
        {
            if (this.autoExpandCheckBox.IsChecked == true)
            {
                item.IsExpanded = true;
            }

            this.Invoke($"cd {ShellConstants.DriveName}:\\{item.NodePath()}", showPrompt: this.isSettingLocationFromLocationProvider == false);
        }

        private void FindAndLoadProfile(string scriptFolder)
        {
            if (this.LoadProfile(Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ShellConstants.SnoopProfile)))
            {
                return;
            }

            if (this.LoadProfile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\WindowsPowerShell", ShellConstants.SnoopProfile)))
            {
                return;
            }

            this.LoadProfile(Path.Combine(scriptFolder, ShellConstants.SnoopProfile));
        }

        private bool LoadProfile(string scriptPath)
        {
            if (File.Exists(scriptPath))
            {
                this.Invoke("write-host ''");
                this.Invoke(string.Format("${0} = '{1}'; . ${0}", ShellConstants.Profile, scriptPath));
                this.Invoke(string.Format("write-host \"Loaded `$profile: ${0}\"", ShellConstants.Profile));

                return true;
            }

            return false;
        }

        private void OnCommandTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    this.SetCommandTextToHistory(++this.historyIndex);
                    break;
                case Key.Down:
                    if (this.historyIndex - 1 <= 0)
                    {
                        this.commandTextBox.Clear();
                    }
                    else
                    {
                        this.SetCommandTextToHistory(--this.historyIndex);
                    }

                    break;
                case Key.Return:
                    // Only append text if there was a command
                    if (string.IsNullOrEmpty(this.commandTextBox.Text) == false)
                    {
                        this.outputTextBox.AppendText(this.commandTextBox.Text);
                    }

                    this.Invoke(this.commandTextBox.Text, true, showPrompt: true);
                    this.commandTextBox.Clear();
                    break;
            }
        }

        private void Invoke(string script, bool addToHistory = false, bool showPrompt = false)
        {
            this.historyIndex = 0;

            if (showPrompt)
            {
                this.outputTextBox.AppendText(Environment.NewLine);
            }

            if (string.IsNullOrEmpty(script) == false)
            {
                try
                {
                    using (var pipe = this.runspace.CreatePipeline(script, addToHistory))
                    {
                        var cmd = new Command("Out-String");
                        cmd.Parameters.Add("Width", Math.Max(2, (int)(this.ActualWidth * 0.7)));
                        pipe.Commands.Add(cmd);

                        foreach (var item in pipe.Invoke())
                        {
                            var textData = item.ToString()?.TrimEnd();
                            this.outputTextBox.AppendText(textData);
                        }

                        foreach (PSObject item in pipe.Error.ReadToEnd())
                        {
                            var error = (ErrorRecord)item.BaseObject;
                            this.OutputErrorRecord(error);
                        }
                    }
                }
                catch (RuntimeException ex)
                {
                    this.OutputErrorRecord(ex.ErrorRecord);
                }
                catch (Exception ex)
                {
                    this.outputTextBox.AppendText(string.Format("Oops! Uncaught exception invoking on the PowerShell runspace: {0}", ex));
                }
            }

            this.outputTextBox.ScrollToEnd();

            if (showPrompt)
            {
                this.outputTextBox.AppendText(Environment.NewLine);

                // ReSharper disable once TailRecursiveCall
                this.Invoke("prompt");
            }
        }

        private void OutputErrorRecord(ErrorRecord error)
        {
            this.outputTextBox.AppendText(Environment.NewLine);
            this.outputTextBox.AppendText(string.Format("{0}{1}", error, error.InvocationInfo != null ? error.InvocationInfo.PositionMessage : string.Empty));
            this.outputTextBox.AppendText(string.Format("{1}  + CategoryInfo          : {0}", error.CategoryInfo, Environment.NewLine));
            this.outputTextBox.AppendText(string.Format("{1}  + FullyQualifiedErrorId : {0}", error.FullyQualifiedErrorId, Environment.NewLine));
        }

        private void SetCommandTextToHistory(int history)
        {
            var cmd = this.GetHistoryCommand(history);
            if (cmd != null)
            {
                this.commandTextBox.Text = cmd;
                this.commandTextBox.SelectionStart = cmd.Length;
            }
        }

        private string GetHistoryCommand(int history)
        {
            using (var pipe = this.runspace.CreatePipeline("get-history -count " + history, false))
            {
                var results = pipe.Invoke();
                if (results.Count > 0)
                {
                    var item = results[0];
                    return (string)item.Properties["CommandLine"].Value;
                }

                return null;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            switch (e.Key)
            {
                case Key.F5:
                    this.Invoke(string.Format("if (${0}) {{ . ${0} }}", ShellConstants.Profile));
                    break;
                case Key.F12:
                    this.outputTextBox.Clear();
                    break;
            }
        }
    }
}