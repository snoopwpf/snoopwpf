namespace Snoop.Infrastructure.Helpers
{
    using System;
    using System.Diagnostics;

    public static class PresentationTraceSourcesHelper
    {
        private static bool alreadyRefreshed;

        public static void RefreshAndEnsureInformationLevel(bool forceRefresh = false)
        {
            // wrap the following PresentationTraceSources.Refresh() call in a try/catch
            // sometimes a NullReferenceException occurs
            // due to empty <filter> elements in the app.config file of the app you are snooping
            try
            {
                if (alreadyRefreshed == false
                    || forceRefresh)
                {
                    PresentationTraceSources.Refresh();
                }
            }
            catch (Exception exception)
            {
                // swallow all exceptions since you can snoop just fine anyways and we don't want the process to crash
                Trace.WriteLine(exception);
            }
            finally
            {
                alreadyRefreshed = true;
            }

            EnsureInformationLevel();
        }

        public static void EnsureInformationLevel()
        {
            // to get all failed binding results we have to increase the trace level
            if (PresentationTraceSources.DataBindingSource.Switch.Level < SourceLevels.Information)
            {
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Information;
            }
        }
    }
}