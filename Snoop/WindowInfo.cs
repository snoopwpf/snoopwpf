namespace Snoop
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Windows.Input;
    using Snoop.Data;
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

		public IList<NativeMethods.MODULEENTRY32> Modules => this.modules ?? (this.modules = this.GetModules().ToList());

	    /// <summary>
		/// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
		/// except that we include 32 bit modules when Snoop runs in 64 bit mode.
		/// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
		/// </summary>
		private IEnumerable<NativeMethods.MODULEENTRY32> GetModules()
		{
		    NativeMethods.GetWindowThreadProcessId(this.HWnd, out var processId);

			var me32 = new NativeMethods.MODULEENTRY32();
			var hModuleSnap = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapshotFlags.Module | NativeMethods.SnapshotFlags.Module32, processId);

		    if (hModuleSnap.IsInvalid)
		    {
		        yield break;
		    }

		    using (hModuleSnap)
		    {
		        me32.dwSize = (uint)Marshal.SizeOf(me32);

		        if (NativeMethods.Module32First(hModuleSnap, ref me32))
		        {
		            do
		            {
		                yield return me32;
		            } while (NativeMethods.Module32Next(hModuleSnap, ref me32));
		        }
		    }
		}

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
					    if (windowClassNameRegex.IsMatch(NativeMethods.GetClassName(this.HWnd)))
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

	    public bool IsOwningProcess64Bit => (this.isOwningProcess64Bit ?? (this.isOwningProcess64Bit = IsProcess64Bit(this.OwningProcess))) == true;

	    public bool IsOwningProcessElevated => (this.isOwningProcessElevated ?? (this.isOwningProcessElevated = IsProcessElevated(this.OwningProcess))) == true;

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

		public override string ToString()
		{
			return this.Description;
		}

		public void Snoop()
		{
			Mouse.OverrideCursor = Cursors.Wait;

			try
			{
				Injector.Launch(this, typeof(SnoopUI).Assembly, typeof(SnoopUI).FullName, "GoBabyGo", new TransientSettingsData(Settings.Default).WriteToFile());
			}
			catch (Exception e)
			{
			    this.OnFailedToAttach(e);
			}

			Mouse.OverrideCursor = null;
		}

		public void Magnify()
		{
			Mouse.OverrideCursor = Cursors.Wait;

			try
			{
				Injector.Launch(this, typeof(Zoomer).Assembly, typeof(Zoomer).FullName, "GoBabyGo", new TransientSettingsData(Settings.Default).WriteToFile());
			}
			catch (Exception e)
			{
			    this.OnFailedToAttach(e);
			}

			Mouse.OverrideCursor = null;
		}

		private void OnFailedToAttach(Exception e)
		{
		    this.AttachFailed?.Invoke(this, new AttachFailedEventArgs(e, this.Description));
		}

	    // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms684139%28v=vs.85%29.aspx
	    public static bool IsProcess64Bit(Process process)
	    {
            if (Environment.Is64BitOperatingSystem == false)
            {
                return false;
            }

            // if this method is not available in your version of .NET, use GetNativeSystemInfo via P/Invoke instead
	        using (var processHandle = NativeMethods.OpenProcess(process, NativeMethods.ProcessAccessFlags.QueryLimitedInformation))
	        {
	            if (processHandle.IsInvalid)
	            {
	                throw new Exception("Could not query process information.");
	            }

	            if (NativeMethods.IsWow64Process(processHandle.DangerousGetHandle(), out var isWow64) == false)
	            {
	                throw new Win32Exception();
	            }

	            return isWow64 == false;
	        }
	    }

	    private static bool IsProcessElevated(Process process)
	    {
	        using (var processHandle = NativeMethods.OpenProcess(process, NativeMethods.ProcessAccessFlags.QueryInformation))
	        {
	            if (processHandle.IsInvalid)
	            {
	                var error = Marshal.GetLastWin32Error();

	                return error == NativeMethods.ERROR_ACCESS_DENIED;
	            }

	            return false;
	        }
	    }
	}
}
