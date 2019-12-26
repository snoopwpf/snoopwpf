namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;
    using JetBrains.Annotations;
    using Snoop.Data;
    using Snoop.Infrastructure.Helpers;
    using Snoop.Windows;

    public class SnoopManager : MarshalByRefObject
    {
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

            IList<AppDomain> appDomains = null;

            if (settingsData.MultipleAppDomainMode != MultipleAppDomainMode.NeverUse)
            {
                appDomains = new AppDomainHelper().GetAppDomains();
            }

            var numberOfAppDomains = appDomains?.Count ?? 1;
            var succeeded = false;

            if (numberOfAppDomains <= 1)
            {
                Trace.WriteLine("Snoop wasn't able to enumerate app domains or MultipleAppDomainMode was disabled. Trying to run in single app domain mode.");

                succeeded = this.GoBabyGoForCurrentAppDomain(settingsData);
            }
            else if (numberOfAppDomains == 1)
            {
                Trace.WriteLine("Only found one app domain. Running in single app domain mode.");

                succeeded = this.GoBabyGoForCurrentAppDomain(settingsData);
            }
            else
            {
                Trace.WriteLine($"Found {numberOfAppDomains} app domains. Running in multiple app domain mode.");

                var shouldUseMultipleAppDomainMode = settingsData.MultipleAppDomainMode == MultipleAppDomainMode.Ask
                                                     || settingsData.MultipleAppDomainMode == MultipleAppDomainMode.AlwaysUse;
                if (settingsData.MultipleAppDomainMode == MultipleAppDomainMode.Ask)
                {
                    var result =
                        MessageBox.Show
                        (
                            "Snoop has noticed multiple app domains.\n\n" +
                            "Would you like to enter multiple app domain mode, and have a separate Snoop window for each app domain?\n\n" +
                            "Without having a separate Snoop window for each app domain, you will not be able to Snoop the windows in the app domains outside of the main app domain. ",
                            "Enter Multiple AppDomain Mode",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                    if (result != MessageBoxResult.Yes)
                    {
                        shouldUseMultipleAppDomainMode = false;
                    }
                }

                if (shouldUseMultipleAppDomainMode == false
                    || appDomains == null)
                {
                    succeeded = this.GoBabyGoForCurrentAppDomain(settingsData);
                }
                else
                {
                    SnoopModes.MultipleAppDomainMode = true;

                    var assemblyFullName = typeof(SnoopManager).Assembly.Location;
                    var fullName = typeof(SnoopManager).FullName;

                    foreach (var appDomain in appDomains)
                    {
                        var crossAppDomainSnoop = (SnoopManager)appDomain.CreateInstanceFromAndUnwrap(assemblyFullName, fullName);
                        //runs in a separate AppDomain
                        var appDomainSucceeded = crossAppDomainSnoop.GoBabyGoForCurrentAppDomain(settingsData);
                        succeeded = succeeded || appDomainSucceeded;
                    }
                }
            }

            if (succeeded == false)
            {
                MessageBox.Show
                    (
                        "Can't find a current application or a PresentationSource root visual.",
                        "Can't Snoop",
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation
                    );
            }

            return succeeded;
        }

        // We have to wrap the call to SnoopUI.GoBabyGoForCurrentAppDomain in an instance member.
        // Otherwise we are not called in the desired appdomain.
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool GoBabyGoForCurrentAppDomain(TransientSettingsData settingsData)
        {
            Trace.WriteLine($"Running snoop in app domain \"{AppDomain.CurrentDomain.FriendlyName}\".");

            try
            {
                var instanceCreator = GetInstanceCreator(settingsData.StartTarget);

                SnoopApplication(settingsData, instanceCreator);
            }
            catch (Exception exception)
            {
                ErrorDialog.ShowDialog(exception, "Error Snooping", "There was an error snooping the application.", exceptionAlreadyHandled: true);
                return false;
            }

            return true;
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

        private static void SnoopApplication(TransientSettingsData settingsData, Func<SnoopMainBaseWindow> instanceCreator)
        {
            Trace.WriteLine("Snooping application.");

            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            if (dispatcher.CheckAccess())
            {
                Trace.WriteLine("Starting snoop UI...");

                var targetWindow = WindowHelper.GetVisibleWindow(settingsData.TargetWindowHandle, dispatcher);

                var snoop = instanceCreator();

                snoop.Title = TryGetWindowOrMainWindowTitle(targetWindow);

                if (string.IsNullOrEmpty(snoop.Title))
                {
                    snoop.Title = "Snoop";
                }
                else
                {
                    snoop.Title += " - Snoop";
                }

                snoop.Inspect();

                if (targetWindow != null)
                {
                    snoop.Target = targetWindow;
                }

                CheckForOtherDispatchers(dispatcher, settingsData, instanceCreator);
            }
            else
            {
                Trace.WriteLine("Current dispatcher runs on a different thread.");

                dispatcher.Invoke((Action)(() => SnoopApplication(settingsData, instanceCreator)));
            }
        }

        private static string TryGetWindowOrMainWindowTitle(Window targetWindow)
        {
            if (targetWindow != null)
            {
                return targetWindow.Title;
            }

            if (Application.Current != null
                && Application.Current.MainWindow != null)
            {
                return Application.Current.MainWindow.Title;
            }

            return string.Empty;
        }

        private static void CheckForOtherDispatchers(Dispatcher mainDispatcher, TransientSettingsData settingsData, Func<SnoopMainBaseWindow> instanceCreator)
        {
            if (settingsData.MultipleDispatcherMode == MultipleDispatcherMode.NeverUse)
            {
                return;
            }

            // check and see if any of the root visuals have a different mainDispatcher
            // if so, ask the user if they wish to enter multiple mainDispatcher mode.
            // if they do, launch a snoop ui for every additional mainDispatcher.
            // see http://snoopwpf.codeplex.com/workitem/6334 for more info.

            var rootVisuals = new List<Visual>();
            var dispatchers = new List<Dispatcher>
                              {
                                  mainDispatcher
                              };

            foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
            {
                var presentationSourceRootVisual = presentationSource.RootVisual;

                if (!(presentationSourceRootVisual is Window))
                {
                    continue;
                }

                var presentationSourceRootVisualDispatcher = presentationSourceRootVisual.Dispatcher;

                if (dispatchers.IndexOf(presentationSourceRootVisualDispatcher) == -1)
                {
                    rootVisuals.Add(presentationSourceRootVisual);
                    dispatchers.Add(presentationSourceRootVisualDispatcher);
                }
            }

            var useMultipleDispatcherMode = false;
            if (rootVisuals.Count > 0)
            {
                // Should we skip the question and always use multiple dispatcher mode?
                if (settingsData.MultipleDispatcherMode == MultipleDispatcherMode.AlwaysUse)
                {
                    useMultipleDispatcherMode = true;
                }
                else
                {
                    var result =
                        MessageBox.Show
                        (
                            "Snoop has noticed windows running in multiple dispatchers!\n\n" +
                            "Would you like to enter multiple dispatcher mode, and have a separate Snoop window for each dispatcher?\n\n" +
                            "Without having a separate Snoop window for each dispatcher, you will not be able to Snoop the windows in the dispatcher threads outside of the main dispatcher. " +
                            "Also, note, that if you bring up additional windows in additional dispatchers (after snooping), you will need to Snoop again in order to launch Snoop windows for those additional dispatchers.",
                            "Enter Multiple Dispatcher Mode",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                    if (result == MessageBoxResult.Yes)
                    {
                        useMultipleDispatcherMode = true;
                    }
                }

                if (useMultipleDispatcherMode)
                {
                    SnoopModes.MultipleDispatcherMode = true;
                    var thread = new Thread(DispatchOut);
                    thread.Start(new DispatchOutParameters(settingsData, instanceCreator, rootVisuals));
                }
            }
        }

        private static void DispatchOut(object o)
        {
            var dispatchOutParameters = (DispatchOutParameters)o;

            foreach (var visual in dispatchOutParameters.Visuals)
            {
                if (visual.Dispatcher == null)
                {
                    Trace.WriteLine($"\"{ObjectToStringConverter.Instance.Convert(visual)}\" has no dispatcher.");
                    continue;
                }

                // launch a snoop ui on each dispatcher
                visual.Dispatcher.Invoke
                (
                    (Action)
                    (
                        () =>
                        {
                            var snoopOtherDispatcher = dispatchOutParameters.InstanceCreator();
                            snoopOtherDispatcher.Inspect(visual);
                        }
                    )
                );
            }
        }

        private class DispatchOutParameters
        {
            public DispatchOutParameters(TransientSettingsData settingsData, Func<SnoopMainBaseWindow> instanceCreator, List<Visual> visuals)
            {
                this.SettingsData = settingsData;
                this.InstanceCreator = instanceCreator;
                this.Visuals = visuals;
            }

            public TransientSettingsData SettingsData { get; }

            public Func<SnoopMainBaseWindow> InstanceCreator { get; }

            public List<Visual> Visuals { get; }
        }
    }
}