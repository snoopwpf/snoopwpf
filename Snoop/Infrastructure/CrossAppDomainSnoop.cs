namespace Snoop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using Snoop.Data;
    using Snoop.Infrastructure;
    using Snoop.mscoree;

    public class CrossAppDomainSnoop : MarshalByRefObject
    {
        private IList<AppDomain> appDomains;
        private AutoResetEvent autoResetEvent;

        public bool CrossDomainGoBabyGo(string settingsFile)
        {
            TransientSettingsData.LoadCurrentIfRequired(settingsFile);

            if (TransientSettingsData.Current.MultipleAppDomainMode == MultipleAppDomainMode.AlwaysUse
                || TransientSettingsData.Current.MultipleAppDomainMode == MultipleAppDomainMode.Ask)
            {
                var staThread = new Thread(this.EnumAppDomains);
                staThread.SetApartmentState(ApartmentState.STA); //STA is required when enumerating app domains
                this.autoResetEvent = new AutoResetEvent(false);
                staThread.Start();

                this.autoResetEvent.WaitOne();
            }

            var numberOfAppDomains = this.appDomains?.Count ?? 1;
            var succeeded = false;

            if (numberOfAppDomains <= 1)
            {
                Trace.WriteLine("Snoop wasn't able to enumerate app domains or MultipleAppDomainMode was disabled. Trying to run in single app domain mode.");

                succeeded = SnoopUI.GoBabyGoForCurrentAppDomain(settingsFile);
            }
            else if (numberOfAppDomains == 1)
            {
                Trace.WriteLine("Only found one app domain. Running in single app domain mode.");

                succeeded = SnoopUI.GoBabyGoForCurrentAppDomain(settingsFile);
            }
            else
            {
                Trace.WriteLine($"Found {numberOfAppDomains} app domains. Running in multiple app domain mode.");

                var shouldUseMultipleAppDomainMode = true;
                if (TransientSettingsData.Current.MultipleAppDomainMode == MultipleAppDomainMode.Ask)
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
                    || this.appDomains == null)
                {
                    succeeded = SnoopUI.GoBabyGoForCurrentAppDomain(settingsFile);
                }
                else
                {
                    SnoopModes.MultipleAppDomainMode = true;

                    var assemblyFullName = typeof(CrossAppDomainSnoop).Assembly.Location;
                    var fullName = typeof(CrossAppDomainSnoop).FullName;

                    foreach (var appDomain in this.appDomains)
                    {
                        var crossAppDomainSnoop = (CrossAppDomainSnoop)appDomain.CreateInstanceFromAndUnwrap(assemblyFullName, fullName);
                        //runs in a separate AppDomain
                        var appDomainSucceeded = crossAppDomainSnoop.GoBabyGoForCurrentAppDomain(settingsFile);
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
        private bool GoBabyGoForCurrentAppDomain(string settingsFile)
        {
            return SnoopUI.GoBabyGoForCurrentAppDomain(settingsFile);
        }

        private void EnumAppDomains()
        {
            this.appDomains = GetAppDomains();
            this.autoResetEvent.Set();
        }

        private static IList<AppDomain> GetAppDomains()
        {
            IList<AppDomain> result = new List<AppDomain>();
            var enumHandle = IntPtr.Zero;
            var runtimeHost = new CorRuntimeHost();
            try
            {
                runtimeHost.EnumDomains(out enumHandle);

                while (true)
                {
                    runtimeHost.NextDomain(enumHandle, out var domain);

                    if (domain == null)
                    {
                        break;
                    }

                    var appDomain = (AppDomain)domain;
                    result.Add(appDomain);
                }

                return result;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                return null;
            }
            finally
            {
                runtimeHost.CloseEnum(enumHandle);
                Marshal.ReleaseComObject(runtimeHost);
            }
        }
    }
}