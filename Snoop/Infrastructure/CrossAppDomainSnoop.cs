namespace Snoop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using Snoop.Infrastructure;
    using Snoop.mscoree;

    public class CrossAppDomainSnoop : MarshalByRefObject
    {
        private IList<AppDomain> appDomains;
        private AutoResetEvent autoResetEvent;

        public bool CrossDomainGoBabyGo()
        {
            var staThread = new Thread(this.EnumAppDomains);
            staThread.SetApartmentState(ApartmentState.STA); //STA is required when enumerating app domains
            this.autoResetEvent = new AutoResetEvent(false);
            staThread.Start();

            this.autoResetEvent.WaitOne();

            var succeeded = false;

            if (this.appDomains == null 
                || this.appDomains.Count == 0)
            {
                Trace.WriteLine("Snoop wasn't able to enumerate app domains. Trying to run in single app domain mode.");

                succeeded = this.RunGoBabyGo();
            }
            else if (this.appDomains.Count == 1)
            {
                succeeded = this.RunGoBabyGo();
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
                    var appDomainSucceeded = crossAppDomainSnoop.RunGoBabyGo();
                    succeeded = succeeded || appDomainSucceeded;
                }
            }

            if (!succeeded)
            {
                MessageBox.Show
                    (
                        "Can't find a current application or a PresentationSource root visual!",
                        "Can't Snoop",
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation
                    );
            }

            return succeeded;
        }

        private void EnumAppDomains()
        {
            this.appDomains = GetAppDomains();
            this.autoResetEvent.Set();
        }

        //intended to run in a separate appdomain
        public bool RunGoBabyGo()
        {
            return SnoopUI.GoBabyGoSingleAppDomain();
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