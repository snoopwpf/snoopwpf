// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Snoop
{
    public partial class AppChooser
	{
		static AppChooser()
		{
			RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
		}

		public AppChooser()
		{
			this.Windows = CollectionViewSource.GetDefaultView(this.windows);

			this.InitializeComponent();

			this.CommandBindings.Add(new CommandBinding(RefreshCommand, this.HandleRefreshCommand));
			this.CommandBindings.Add(new CommandBinding(InspectCommand, this.HandleInspectCommand, this.HandleCanInspectOrMagnifyCommand));
			this.CommandBindings.Add(new CommandBinding(MagnifyCommand, this.HandleMagnifyCommand, this.HandleCanInspectOrMagnifyCommand));
			this.CommandBindings.Add(new CommandBinding(MinimizeCommand, this.HandleMinimizeCommand));
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, this.HandleCloseCommand));
		}

		public static readonly RoutedCommand InspectCommand = new RoutedCommand();
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand();
		public static readonly RoutedCommand MagnifyCommand = new RoutedCommand();
		public static readonly RoutedCommand MinimizeCommand = new RoutedCommand();

        public ICollectionView Windows { get; }

        private readonly ObservableCollection<WindowInfo> windows = new ObservableCollection<WindowInfo>();

		public void Refresh()
		{
			this.windows.Clear();

		    this.Dispatcher.BeginInvoke
			(
				DispatcherPriority.Loaded,
				(DispatcherOperationCallback)delegate
				{
					try
					{
						Mouse.OverrideCursor = Cursors.Wait;

						foreach (var windowHandle in NativeMethods.ToplevelWindows)
						{
							var window = new WindowInfo(windowHandle);
							if (window.IsValidProcess 
							    && !this.HasProcess(window.OwningProcess))
							{
								new AttachFailedHandler(window, this);
								this.windows.Add(window);
							}
						}

						if (this.windows.Count > 0)
                        {
                            this.Windows.MoveCurrentTo(this.windows[0]);
                        }
                    }
					finally
					{
						Mouse.OverrideCursor = null;
					}
					return null;
				},
				null
			);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

		    // load the window placement details from the user settings.
		    SnoopWindowUtils.LoadWindowPlacement(this, Properties.Settings.Default.AppChooserWindowPlacement);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

		    // persist the window placement details to the user settings.
            SnoopWindowUtils.SaveWindowPlacement(this, wp => Properties.Settings.Default.AppChooserWindowPlacement = wp);
		
			Properties.Settings.Default.Save();
		}

		private bool HasProcess(Process process)
		{
			foreach (var window in this.windows)
            {
                if (window.OwningProcess.Id == process.Id)
                {
                    return true;
                }
            }

            return false;
		}

		private void HandleCanInspectOrMagnifyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			if (this.Windows.CurrentItem != null)
            {
                e.CanExecute = true;
            }

            e.Handled = true;
		}

		private void HandleInspectCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var window = (WindowInfo)this.Windows.CurrentItem;
		    window?.Snoop();
		}

		private void HandleMagnifyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var window = (WindowInfo)this.Windows.CurrentItem;
		    window?.Magnify();
		}

		private void HandleRefreshCommand(object sender, ExecutedRoutedEventArgs e)
		{
			// clear out cached process info to make the force refresh do the process check over again.
			WindowInfo.ClearCachedWindowHandleInfo();
			this.Refresh();
		}

		private void HandleMinimizeCommand(object sender, ExecutedRoutedEventArgs e)
		{
			this.WindowState = System.Windows.WindowState.Minimized;
		}

		private void HandleCloseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			this.DragMove();
		}
	}

	public class WindowInfo
	{
		public WindowInfo(IntPtr hwnd)
		{
			this.hwnd = hwnd;			
		}

		public static void ClearCachedProcessInfo()
		{
			WindowInfo.processIDToValidityMap.Clear();
		}

		public event EventHandler<AttachFailedEventArgs> AttachFailed;

		public IEnumerable<NativeMethods.MODULEENTRY32> Modules
		{
			get
			{
				if (_modules == null)
					_modules = GetModules().ToArray();
				return _modules;
			}
		}
		/// <summary>
		/// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
		/// except that we include 32 bit modules when Snoop runs in 64 bit mode.
		/// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
		/// </summary>
		private IEnumerable<NativeMethods.MODULEENTRY32> GetModules()
		{
			int processId;
			NativeMethods.GetWindowThreadProcessId(hwnd, out processId);

			var me32 = new NativeMethods.MODULEENTRY32();
			var hModuleSnap = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapshotFlags.Module | NativeMethods.SnapshotFlags.Module32, processId);
			if (!hModuleSnap.IsInvalid)
			{
				using (hModuleSnap)
				{
					me32.dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(me32);
					if (NativeMethods.Module32First(hModuleSnap, ref me32))
					{
						do
						{
							yield return me32;
						} while (NativeMethods.Module32Next(hModuleSnap, ref me32));
					}
				}
			}
		}
		private IEnumerable<NativeMethods.MODULEENTRY32> _modules;

		public bool IsValidProcess
		{
			get
			{
				bool isValid = false;
				try
				{
					if (this.hwnd == IntPtr.Zero)
						return false;

					Process process = this.OwningProcess;
					if (process == null)
						return false;

					// see if we have cached the process validity previously, if so, return it.
					if (WindowInfo.processIDToValidityMap.TryGetValue(process.Id, out isValid))
						return isValid;

					// else determine the process validity and cache it.
					if (process.Id == Process.GetCurrentProcess().Id)
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

						foreach (var module in Modules)
						{
							if
							(
								module.szModule.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
								module.szModule.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
								module.szModule.StartsWith("wpfgfx", StringComparison.OrdinalIgnoreCase)
							)
							{
								isValid = true;
								break;
							}
						}
					}

					WindowInfo.processIDToValidityMap[process.Id] = isValid;
				}
				catch(Exception) {}
				return isValid;
			}
		}
		public Process OwningProcess
		{
			get { return NativeMethods.GetWindowThreadProcess(this.hwnd); }
		}
		public IntPtr HWnd
		{
			get { return this.hwnd; }
		}
		private IntPtr hwnd;
		public string Description
		{
			get
			{
				Process process = this.OwningProcess;
				return process.MainWindowTitle + " - " + process.ProcessName + " [" + process.Id.ToString() + "]";
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
				Injector.Launch(this.HWnd, typeof(SnoopUI).Assembly, typeof(SnoopUI).FullName, "GoBabyGo", new TransientSettingsData(Settings.Default).WriteToFile());
            }
			catch (Exception e)
			{
				OnFailedToAttach(e);
			}
			Mouse.OverrideCursor = null;
		}

	    public void Magnify()
		{
			Mouse.OverrideCursor = Cursors.Wait;
			try
			{
				Injector.Launch(this.HWnd, typeof(Zoomer).Assembly, typeof(Zoomer).FullName, "GoBabyGo", new TransientSettingsData(Settings.Default).WriteToFile());
			}
			catch (Exception e)
			{
				OnFailedToAttach(e);
			}
			Mouse.OverrideCursor = null;
		}

		private void OnFailedToAttach(Exception e)
		{
			var handler = AttachFailed;
			if (handler != null)
			{
				handler(this, new AttachFailedEventArgs(e, this.Description));
			}
		}
		
		private static Dictionary<int, bool> processIDToValidityMap = new Dictionary<int, bool>();
	}

	public class AttachFailedEventArgs : EventArgs
	{
		public AttachFailedEventArgs(Exception attachException, string windowName)
		{
		    this.AttachException = attachException;
		    this.WindowName = windowName;
		}

	    public Exception AttachException { get; }

	    public string WindowName { get; }
	}

	public class AttachFailedHandler
	{
		public AttachFailedHandler(WindowInfo window, AppChooser appChooser = null)
		{
			window.AttachFailed += this.OnSnoopAttachFailed;
			this.appChooser = appChooser;
		}

		private void OnSnoopAttachFailed(object sender, AttachFailedEventArgs e)
		{
            ErrorDialog.ShowDialog(e.AttachException, "Can't Snoop the process", $"Failed to attach to '{e.WindowName}'.", exceptionAlreadyHandled: true);
	        
		    // TODO This should be implemented through the event broker, not like this.
		    this.appChooser?.Refresh();
		}

		private readonly AppChooser appChooser;
	}
}