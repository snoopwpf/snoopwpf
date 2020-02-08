namespace Snoop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Input;
    using Snoop.Data;
    using Snoop.Infrastructure;
    using Snoop.Properties;

    public class WindowInfo
    {
        private static readonly Dictionary<IntPtr, bool> windowHandleToValidityMap = new Dictionary<IntPtr, bool>();

        // we have to match "HwndWrapper[{0};{1};{2}]" which is used at https://referencesource.microsoft.com/#WindowsBase/Shared/MS/Win32/HwndWrapper.cs,2a8e13c293bb3f8c
        private static readonly Regex windowClassNameRegex = new Regex(@"^HwndWrapper\[.*;.*;.*\]$", RegexOptions.Compiled);

        private IList<NativeMethods.MODULEENTRY32> modules;
        private Process owningProcess;
        private bool? isOwningProcess64Bit;
        private bool? isOwningProcessElevated;
        private static readonly int snoopProcessId = Process.GetCurrentProcess().Id;

        public WindowInfo(IntPtr hwnd)
        {
            this.HWnd = hwnd;
        }

        public event EventHandler<AttachFailedEventArgs> AttachFailed;

        public static void ClearCachedWindowHandleInfo()
        {
            windowHandleToValidityMap.Clear();
        }

        public IList<NativeMethods.MODULEENTRY32> Modules => this.modules ?? (this.modules = NativeMethods.GetModulesFromWindowHandle(this.HWnd).ToList());

        public bool IsValidProcess
        {
            get
            {
                var isValid = false;
                try
                {
                    if (this.HWnd == IntPtr.Zero)
                    {
                        return false;
                    }

                    // see if we have cached the process validity previously, if so, return it.
                    if (windowHandleToValidityMap.TryGetValue(this.HWnd, out isValid))
                    {
                        return isValid;
                    }

                    var process = this.OwningProcess;
                    if (process == null)
                    {
                        return false;
                    }

                    // else determine the process validity and cache it.
                    if (process.Id == snoopProcessId)
                    {
                        isValid = false;

                        // the above line stops the user from snooping on snoop, since we assume that ... that isn't their goal.
                        // to get around this, the user can bring up two snoops and use the second snoop ... to snoop the first snoop.
                        // well, that let's you snoop the app chooser. in order to snoop the main snoop ui, you have to bring up three snoops.
                        // in this case, bring up two snoops, as before, and then bring up the third snoop, using it to snoop the first snoop.
                        // since the second snoop inserted itself into the first snoop's process, you can now spy the main snoop ui from the
                        // second snoop (bring up another main snoop ui to do so). pretty tricky, huh! and useful!
                    }
                    else
                    {
                        // WPF-Windows have a defined class name
                        if (windowClassNameRegex.IsMatch(this.ClassName))
                        {
                            isValid = true;
                        }

                        if (isValid == false)
                        {
                            // a process is valid to snoop if it contains a dependency on PresentationFramework, PresentationCore, or milcore (wpfgfx).
                            // this includes the files:
                            // PresentationFramework.dll, PresentationFramework.ni.dll
                            // PresentationCore.dll, PresentationCore.ni.dll
                            // wpfgfx_v0300.dll (WPF 3.0/3.5)
                            // wpfgrx_v0400.dll (WPF 4.0)

                            // note: sometimes PresentationFramework.dll doesn't show up in the list of modules.
                            // so, it makes sense to also check for the unmanaged milcore component (wpfgfx_vxxxx.dll).
                            // see for more info: http://snoopwpf.codeplex.com/Thread/View.aspx?ThreadId=236335

                            // sometimes the module names aren't always the same case. compare case insensitive.
                            // see for more info: http://snoopwpf.codeplex.com/workitem/6090

                            foreach (var module in this.Modules)
                            {
                                if (module.szModule.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase)
                                    || module.szModule.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase)
                                    || module.szModule.StartsWith("wpfgfx", StringComparison.OrdinalIgnoreCase))
                                {
                                    isValid = true;
                                    break;
                                }
                            }
                        }
                    }

                    windowHandleToValidityMap[this.HWnd] = isValid;
                }
                catch
                {
                    // ignored
                }

                return isValid;
            }
        }

        public Process OwningProcess => this.owningProcess ?? (this.owningProcess = NativeMethods.GetWindowThreadProcess(this.HWnd));

        public bool IsOwningProcess64Bit => (this.isOwningProcess64Bit ?? (this.isOwningProcess64Bit = NativeMethods.IsProcess64Bit(this.OwningProcess))) == true;

        public bool IsOwningProcessElevated => (this.isOwningProcessElevated ?? (this.isOwningProcessElevated = NativeMethods.IsProcessElevated(this.OwningProcess))) == true;

        public IntPtr HWnd { get; }

        public string Description
        {
            get
            {
                var process = this.OwningProcess;
                var windowTitle = NativeMethods.GetText(this.HWnd);

                if (string.IsNullOrEmpty(windowTitle))
                {
                    try
                    {
                        windowTitle = process.MainWindowTitle;
                    }
                    catch (InvalidOperationException)
                    {
                        // The process closed while we were trying to evaluate it
                        return string.Empty;
                    }
                }

                return $"{windowTitle} - {process.ProcessName} [{process.Id}]";
            }
        }

        public string ClassName => NativeMethods.GetClassName(this.HWnd);

        public string TraceInfo => $"{this.Description} [{this.HWnd.ToInt64():X8}] {this.ClassName}";

        public override string ToString()
        {
            return this.Description;
        }

        public void Snoop()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                InjectorLauncherManager.Launch(this, typeof(SnoopManager).GetMethod(nameof(SnoopManager.StartSnoop)), CreateTransientSettingsData(SnoopStartTarget.SnoopUI, this.HWnd));
            }
            catch (Exception e)
            {
                this.OnFailedToAttach(e);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        public void Magnify()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                InjectorLauncherManager.Launch(this, typeof(SnoopManager).GetMethod(nameof(SnoopManager.StartSnoop)), CreateTransientSettingsData(SnoopStartTarget.Zoomer, this.HWnd));
            }
            catch (Exception e)
            {
                this.OnFailedToAttach(e);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void OnFailedToAttach(Exception e)
        {
            this.AttachFailed?.Invoke(this, new AttachFailedEventArgs(e, this.Description));
        }

        private static TransientSettingsData CreateTransientSettingsData(SnoopStartTarget startTarget, IntPtr targetWindowHandle)
        {
            var settings = Settings.Default;

            return new TransientSettingsData
            {
                StartTarget = startTarget,
                TargetWindowHandle = targetWindowHandle.ToInt64(),

                MultipleAppDomainMode = settings.MultipleAppDomainMode,
                MultipleDispatcherMode = settings.MultipleDispatcherMode,
                SetWindowOwner = settings.SetOwnerWindow
            };
        }
    }
}
