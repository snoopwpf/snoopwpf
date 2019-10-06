namespace Snoop.Windows
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Forms.Integration;
    using System.Windows.Threading;
    using Snoop.Infrastructure;

    public abstract class SnoopMainBaseWindow : SnoopBaseWindow
    {
        public bool Inspect()
        {
            var foundRoot = this.FindRoot();
            if (foundRoot == null)
            {
                if (SnoopModes.MultipleDispatcherMode == false
                    && SnoopModes.MultipleAppDomainMode == false)
                {
                    //SnoopModes.MultipleDispatcherMode is always false for all scenarios except for cases where we are running multiple dispatchers.
                    //If SnoopModes.MultipleDispatcherMode was set to true, then there definitely was a root visual found in another dispatcher, so
                    //the message below would be wrong.
                    MessageBox.Show
                        (
                         "Can't find a current application or a PresentationSource root visual.",
                         "Can't Snoop",
                         MessageBoxButton.OK,
                         MessageBoxImage.Exclamation
                        );
                }

                // This path should only be hit if we don't find a root in some dispatcher or app domain.
                // This is not really critical as not every dispatcher/app domain must meet this requirement.
                Trace.WriteLine("Can't find a current application or a PresentationSource root visual.");

                return false;
            }

            this.Inspect(foundRoot);

            return true;
        }

        public void Inspect(object rootToInspect)
        {
            this.Dispatcher.UnhandledException += this.UnhandledExceptionHandler;

            this.Load(rootToInspect);

            this.Owner = SnoopWindowUtils.FindOwnerWindow(this);

            Trace.WriteLine("Showing snoop UI...");

            this.Show();
            this.Activate();

            Trace.WriteLine("Shown and activated snoop UI.");
        }

        protected abstract void Load(object rootToInspect);

        protected virtual object FindRoot()
        {
            object foundRoot = null;

            if (SnoopModes.MultipleDispatcherMode)
            {
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    if (presentationSource.RootVisual is UIElement element 
                        && element.Dispatcher.CheckAccess())
                    {
                        foundRoot = presentationSource.RootVisual;
                        break;
                    }
                }
            }
            else if (Application.Current != null)
            {
                foundRoot = Application.Current;
            }
            else
            {
                // if we don't have a current application,
                // then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).

                // in this case, let's iterate over PresentationSource.CurrentSources,
                // and use the first non-null, visible RootVisual we find as root to inspect.
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    if (presentationSource.RootVisual is UIElement element 
                        && element.Visibility == Visibility.Visible)
                    {
                        foundRoot = presentationSource.RootVisual;
                        break;
                    }
                }
            }

            if (System.Windows.Forms.Application.OpenForms.Count > 0)
            {
                // this is windows forms -> wpf interop

                // call ElementHost.EnableModelessKeyboardInterop to allow the Snoop UI window
                // to receive keyboard messages. if you don't call this method,
                // you will be unable to edit properties in the property grid for windows forms interop.
                ElementHost.EnableModelessKeyboardInterop(this);
            }

            return foundRoot;
        }

        private void UnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (SnoopModes.IgnoreExceptions)
            {
                return;
            }

            if (SnoopModes.SwallowExceptions)
            {
                e.Handled = true;
                return;
            }

            e.Handled = ErrorDialog.ShowDialog(e.Exception);
        }
    }
}
