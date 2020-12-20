namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;
    using JetBrains.Annotations;
    using Snoop.Data;
    using Snoop.Infrastructure.Helpers;
    using Snoop.Windows;

    [Serializable]
    public class SnoopCrossAppDomainInjector : MarshalByRefObject
    {
        public SnoopCrossAppDomainInjector()
        {
            SnoopModes.MultipleAppDomainMode = true;

            // We have to do this in the constructor because we might not be able to cast correctly.
            // Not being able to cast might be the case if the app domain we should run in uses shadow copies for it's assemblies.
            var settingsFile = Environment.GetEnvironmentVariable("Snoop.SettingsFile");

            if (string.IsNullOrEmpty(settingsFile) == false)
            {
                this.RunInCurrentAppDomain(settingsFile);
            }
        }

        private void RunInCurrentAppDomain(string settingsFile)
        {
            var settingsData = TransientSettingsData.LoadCurrent(settingsFile);
            new SnoopManager().RunInCurrentAppDomain(settingsData);
        }
    }

    public class SnoopManager
    {
        /// <summary>
        /// This is the main entry point being called by the GenericInjector.
        /// </summary>
        /// <param name="settingsFile">Full path to a file containing our <see cref="TransientSettingsData"/>.</param>
        /// <returns>
        /// <c>0</c> if the injection succeeded.
        /// <c>1</c> if the injection failed with an error.
        /// <c>2</c> if the injection succeeded, but we couldn't find anything for snooping.</returns>
        [PublicAPI]
        public static int StartSnoop(string settingsFile)
        {
            try
            {
                return new SnoopManager().StartSnoopInstance(settingsFile)
                    ? 0
                    : 2;
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception);
                return 1;
            }
        }

        private bool StartSnoopInstance(string settingsFile)
        {
            Trace.WriteLine("Starting snoop...");

            var settingsData = TransientSettingsData.LoadCurrent(settingsFile);

            IList<AppDomain>? appDomains = null;

            if (settingsData.MultipleAppDomainMode != MultipleAppDomainMode.NeverUse)
            {
                appDomains = new AppDomainHelper().GetAppDomains();
            }

            var numberOfAppDomains = appDomains?.Count ?? 1;
            var succeeded = false;

            if (numberOfAppDomains < 1)
            {
                Trace.WriteLine("Snoop wasn't able to enumerate app domains or MultipleAppDomainMode was disabled. Trying to run in single app domain mode.");

                succeeded = this.RunInCurrentAppDomain(settingsData);
            }
            else if (numberOfAppDomains == 1)
            {
                Trace.WriteLine("Only found one app domain. Running in single app domain mode.");

                succeeded = this.RunInCurrentAppDomain(settingsData);
            }
            else
            {
                Trace.WriteLine($"Found {numberOfAppDomains} app domains. Running in multiple app domain mode.");

                var shouldUseMultipleAppDomainMode = settingsData.MultipleAppDomainMode == MultipleAppDomainMode.Ask
                                                     || settingsData.MultipleAppDomainMode == MultipleAppDomainMode.AlwaysUse;
                if (settingsData.MultipleAppDomainMode == MultipleAppDomainMode.Ask)
                {
                    var result =
                        MessageBox.Show(
                            "Snoop has noticed multiple app domains.\n\n" +
                            "Would you like to enter multiple app domain mode, and have a separate Snoop window for each app domain?\n\n" +
                            "Without having a separate Snoop window for each app domain, you will not be able to Snoop the windows in the app domains outside of the main app domain.",
                            "Enter Multiple AppDomain Mode",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            MessageBoxResult.No);

                    if (result != MessageBoxResult.Yes)
                    {
                        shouldUseMultipleAppDomainMode = false;
                    }
                }

                if (shouldUseMultipleAppDomainMode == false
                    || appDomains is null)
                {
                    succeeded = this.RunInCurrentAppDomain(settingsData);
                }
                else
                {
                    SnoopModes.MultipleAppDomainMode = true;

                    // Use environment variable to transport snoop settings file accross multiple app domains
                    Environment.SetEnvironmentVariable("Snoop.SettingsFile", settingsFile, EnvironmentVariableTarget.Process);

                    var assemblyFullName = typeof(SnoopManager).Assembly.Location;
                    var fullInjectorClassName = typeof(SnoopCrossAppDomainInjector).FullName;

                    foreach (var appDomain in appDomains)
                    {
                        Trace.WriteLine($"Trying to create Snoop instance in app domain \"{appDomain.FriendlyName}\"...");

                        try
                        {
                            AttachAssemblyResolveHandler(appDomain);

                            // the injection code runs inside the constructor of SnoopCrossAppDomainManager
                            appDomain.CreateInstanceFrom(assemblyFullName, fullInjectorClassName!);

                            // if there is no exception we consider the injection successful
                            var appDomainSucceeded = true;
                            succeeded = succeeded || appDomainSucceeded;

                            Trace.WriteLine($"Successfully created Snoop instance in app domain \"{appDomain.FriendlyName}\".");
                        }
                        catch (Exception exception)
                        {
                            Trace.WriteLine($"Failed to create Snoop instance in app domain \"{appDomain.FriendlyName}\".");
                            Trace.WriteLine(exception);
                        }
                    }
                }
            }

            if (succeeded == false)
            {
                MessageBox.Show("Can't find a current application or a PresentationSource root visual.",
                                "Can't Snoop",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
            }

            return succeeded;
        }

        public bool RunInCurrentAppDomain(TransientSettingsData settingsData)
        {
            Trace.WriteLine($"Trying to run Snoop in app domain \"{AppDomain.CurrentDomain.FriendlyName}\"...");

            try
            {
                AttachAssemblyResolveHandler(AppDomain.CurrentDomain);

                var instanceCreator = GetInstanceCreator(settingsData.StartTarget);

                var result = InjectSnoopIntoDispatchers(settingsData, (data, dispatcher) => CreateSnoopWindow(data, dispatcher, instanceCreator));

                Trace.WriteLine($"Successfully running Snoop in app domain \"{AppDomain.CurrentDomain.FriendlyName}\".");

                return result;
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"Failed to to run Snoop in app domain \"{AppDomain.CurrentDomain.FriendlyName}\".");

                if (SnoopModes.MultipleAppDomainMode)
                {
                    Trace.WriteLine($"Could not snoop a specific app domain with friendly name of \"{AppDomain.CurrentDomain.FriendlyName}\" in multiple app domain mode.");
                    Trace.WriteLine(exception);
                }
                else
                {
                    ErrorDialog.ShowDialog(exception, "Error snooping", "There was an error snooping the application.", exceptionAlreadyHandled: true);
                }

                return false;
            }
        }

        private static void AttachAssemblyResolveHandler(AppDomain domain)
        {
            try
            {
                domain.AssemblyResolve += HandleDomainAssemblyResolve;
            }
            catch (Exception exception)
            {
                Trace.TraceError("Could not attach assembly resolver. Loading snoop assemblies might fail.");
                Trace.TraceError(exception.ToString());
            }
        }

        private static Assembly? HandleDomainAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (args.Name?.StartsWith("Snoop.Core,") == true)
            {
                return Assembly.GetExecutingAssembly();
            }

#if NETCOREAPP3_1
            if (args.Name?.StartsWith("System.Management.Automation,") == true)
            {
                return Assembly.LoadFrom(Snoop.PowerShell.ShellConstants.GetPowerShellAssemblyPath());
            }
#endif

            return null;
        }

        private static Func<SnoopMainBaseWindow> GetInstanceCreator(SnoopStartTarget startTarget)
        {
            switch (startTarget)
            {
                case SnoopStartTarget.SnoopUI:
                    return () => new SnoopUI();

                case SnoopStartTarget.Zoomer:
                    return () => new Zoomer();

                default:
                    throw new ArgumentOutOfRangeException(nameof(startTarget), startTarget, null);
            }
        }

        private static SnoopMainBaseWindow CreateSnoopWindow(TransientSettingsData settingsData, Dispatcher dispatcher, Func<SnoopMainBaseWindow> instanceCreator)
        {
            var snoopWindow = instanceCreator();

            var targetWindowOnSameDispatcher = WindowHelper.GetVisibleWindow(settingsData.TargetWindowHandle, dispatcher);

            snoopWindow.Title = TryGetWindowOrMainWindowTitle(targetWindowOnSameDispatcher);

            if (string.IsNullOrEmpty(snoopWindow.Title))
            {
                snoopWindow.Title = "Snoop";
            }
            else
            {
                snoopWindow.Title += " - Snoop";
            }

            snoopWindow.Inspect();

            if (targetWindowOnSameDispatcher != null)
            {
                snoopWindow.Target = targetWindowOnSameDispatcher;
            }

            return snoopWindow;
        }

        private static string TryGetWindowOrMainWindowTitle(Window? targetWindow)
        {
            if (targetWindow != null)
            {
                return targetWindow.Title;
            }

            if (Application.Current?.CheckAccess() == true
                && Application.Current?.MainWindow?.CheckAccess() == true)
            {
                return Application.Current.MainWindow.Title;
            }

            return string.Empty;
        }

        private static bool InjectSnoopIntoDispatchers(TransientSettingsData settingsData, Func<TransientSettingsData, Dispatcher, SnoopMainBaseWindow> instanceCreator)
        {
            // Check and see if any of the PresentationSources have different dispatchers.
            // If so, ask the user if they wish to enter multiple dispatcher mode.
            // If they do, launch a snoop ui for every additional dispatcher.
            // See http://snoopwpf.codeplex.com/workitem/6334 for more info.

            var dispatchers = new List<Dispatcher>();

            foreach (PresentationSource? presentationSource in PresentationSource.CurrentSources)
            {
                if (presentationSource is null)
                {
                    continue;
                }

                var presentationSourceDispatcher = presentationSource.Dispatcher;

                // Check if we have already seen this dispatcher
                if (dispatchers.IndexOf(presentationSourceDispatcher) == -1)
                {
                    dispatchers.Add(presentationSourceDispatcher);
                }
            }

            var useMultipleDispatcherMode = false;
            if (dispatchers.Count > 0)
            {
                switch (settingsData.MultipleDispatcherMode)
                {
                    case MultipleDispatcherMode.NeverUse:
                        useMultipleDispatcherMode = false;
                        break;

                    // Should we skip the question and always use multiple dispatcher mode?
                    case MultipleDispatcherMode.AlwaysUse:
                        useMultipleDispatcherMode = dispatchers.Count > 1;
                        break;

                    case MultipleDispatcherMode.Ask:
                    default:
                    {
                        var result =
                            MessageBox.Show(
                                "Snoop has noticed windows running in multiple dispatchers.\n\n" +
                                "Would you like to enter multiple dispatcher mode, and have a separate Snoop window for each dispatcher?\n\n" +
                                "Without having a separate Snoop window for each dispatcher, you will not be able to Snoop the windows in the dispatcher threads outside of the main dispatcher. " +
                                "Also, note, that if you bring up additional windows in additional dispatchers (after snooping), you will need to Snoop again in order to launch Snoop windows for those additional dispatchers.",
                                "Enter Multiple Dispatcher Mode",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            useMultipleDispatcherMode = true;
                        }

                        break;
                    }
                }

                if (useMultipleDispatcherMode)
                {
                    SnoopModes.MultipleDispatcherMode = true;

                    var thread = new Thread(DispatchOut);
                    thread.Start(new DispatchOutParameters(settingsData, instanceCreator, dispatchers));

                    // todo: check if we really created something
                    return true;
                }

                var dispatcher = Application.Current?.Dispatcher ?? dispatchers[0];

                dispatcher.Invoke((Action)(() =>
                {
                    var snoopInstance = instanceCreator(settingsData, dispatcher);

                    snoopInstance.Target ??= GetRootVisual(dispatcher);
                }));

                return true;
            }

            return false;
        }

        private static Visual? GetRootVisual(Dispatcher dispatcher)
        {
            return PresentationSource.CurrentSources
                .OfType<PresentationSource>()
                .FirstOrDefault(x => x.Dispatcher == dispatcher)
                ?.RootVisual;
        }

        private static void DispatchOut(object? o)
        {
            var dispatchOutParameters = (DispatchOutParameters)o!;

            foreach (var dispatcher in dispatchOutParameters.Dispatchers)
            {
                // launch a snoop ui on each dispatcher
                dispatcher.Invoke(
                    (Action)(() =>
                    {
                        var snoopInstance = dispatchOutParameters.InstanceCreator(dispatchOutParameters.SettingsData, dispatcher);

                        snoopInstance.Target ??= GetRootVisual(dispatcher);
                    }));
            }
        }

        private class DispatchOutParameters
        {
            public DispatchOutParameters(TransientSettingsData settingsData, Func<TransientSettingsData, Dispatcher, SnoopMainBaseWindow> instanceCreator, List<Dispatcher> dispatchers)
            {
                this.SettingsData = settingsData;
                this.InstanceCreator = instanceCreator;
                this.Dispatchers = dispatchers;
            }

            public TransientSettingsData SettingsData { get; }

            public Func<TransientSettingsData, Dispatcher, SnoopMainBaseWindow> InstanceCreator { get; }

            public List<Dispatcher> Dispatchers { get; }
        }
    }
}