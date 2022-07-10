namespace Snoop.Infrastructure.Helpers;

#if NETCOREAPP // Appdomains don't exist in .net core
using System;
using System.Collections.Generic;

public class AppDomainHelper
{
    public IList<AppDomain>? GetAppDomains()
    {
        return null;
    }
}
#else
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Snoop.mscoree;

    public class AppDomainHelper
    {
        private IList<AppDomain>? appDomains;
        private AutoResetEvent? autoResetEvent;

        public IList<AppDomain>? GetAppDomains()
        {
            var staThread = new Thread(this.EnumAppDomains)
                {
                    Name = "Snoop_EnumAppDomains_Thread"
                };
            staThread.SetApartmentState(ApartmentState.STA); //STA is required when enumerating app domains
            this.autoResetEvent = new AutoResetEvent(false);
            staThread.Start();

            this.autoResetEvent.WaitOne();

            return this.appDomains;
        }

        private void EnumAppDomains()
        {
            this.appDomains = EnumerateAppDomains();
            this.autoResetEvent!.Set();
        }

        private static IList<AppDomain>? EnumerateAppDomains()
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

                    if (domain is null)
                    {
                        break;
                    }

                    var appDomain = (AppDomain)domain;
                    result.Add(appDomain);
                }

                return result;
            }
            catch (Exception exception)
            {
                LogHelper.WriteWarning(exception);
                return null;
            }
            finally
            {
                runtimeHost.CloseEnum(enumHandle);
                Marshal.ReleaseComObject(runtimeHost);
            }
        }
    }
#endif