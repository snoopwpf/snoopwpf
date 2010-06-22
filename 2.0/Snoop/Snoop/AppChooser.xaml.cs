// Copyright © 2006 Microsoft Corporation.  All Rights Reserved

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ManagedInjector;

namespace Snoop {

	public partial class AppChooser {

		public static readonly RoutedCommand InspectCommand = new RoutedCommand();
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand();
		public static readonly RoutedCommand MagnifyCommand = new RoutedCommand();

		private ObservableCollection<WindowInfo> windows = new ObservableCollection<WindowInfo>();
		private ICollectionView windowsView;

		static AppChooser() {
			AppChooser.RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
		}

		public AppChooser() {
			this.windowsView = CollectionViewSource.GetDefaultView(this.windows);

			this.InitializeComponent();

			this.CommandBindings.Add(new CommandBinding(AppChooser.InspectCommand, this.HandleInspectCommand, this.HandleCanInspectCommand));
			this.CommandBindings.Add(new CommandBinding(AppChooser.RefreshCommand, this.HandleRefreshCommand));
			this.CommandBindings.Add(new CommandBinding(AppChooser.MagnifyCommand, this.HandleMagnifyCommand, this.HandleCanInspectCommand));

			this.Refresh();

			DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromSeconds(20), DispatcherPriority.Background, this.HandleRefreshTimer, Dispatcher.CurrentDispatcher);
		}

		public ICollectionView Windows {
			get { return this.windowsView; }
		}

		public void Refresh() {
			this.windows.Clear();

			foreach (IntPtr windowHandle in NativeMethods.ToplevelWindows) {
				WindowInfo window = new WindowInfo(windowHandle, this);
				if (window.IsValidProcess && !this.HasProcess(window.OwningProcess))
					this.windows.Add(window);
			}

			if (this.windows.Count > 0)
				this.windowsView.MoveCurrentTo(this.windows[0]);
		}

		private bool HasProcess(Process process) {
			foreach (WindowInfo window in this.windows)
				if (window.OwningProcess.Id == process.Id)
					return true;
			return false;
		}

		private void HandleCanInspectCommand(object sender, CanExecuteRoutedEventArgs e) {
			if (this.windowsView.CurrentItem != null)
				e.CanExecute = true;
			e.Handled = true;
		}

		private void HandleInspectCommand(object sender, ExecutedRoutedEventArgs e) {
			WindowInfo window = (WindowInfo)this.windowsView.CurrentItem;
			if (window != null)
				window.Snoop();
		}

		private void HandleMagnifyCommand(object sender, ExecutedRoutedEventArgs e) {
			WindowInfo window = (WindowInfo)this.windowsView.CurrentItem;
			if (window != null)
				window.Magnify();
		}

		private void HandleRefreshCommand(object sender, ExecutedRoutedEventArgs e) {
			// Clear out cached process info to make the force refresh do the process check over again.
			WindowInfo.ClearCachedProcessInfo();
			this.Refresh();
		}

		private void HandleRefreshTimer(object sender, EventArgs e) {
			this.Refresh();
		}

		private void HandleClose(object sender, MouseButtonEventArgs e) {
			this.Close();
		}

		private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			this.DragMove();
		}
	}

	public class WindowInfo
	{
		private static Dictionary<int, bool> processIDToValidityMap = new Dictionary<int, bool>();

		private IntPtr hwnd;
		private AppChooser appChooser;

		public WindowInfo(IntPtr hwnd, AppChooser appChooser) {
			this.hwnd = hwnd;
			this.appChooser = appChooser;
		}

		public static void ClearCachedProcessInfo() {
			WindowInfo.processIDToValidityMap.Clear();
		}

		public bool IsValidProcess {
			get {
				bool isValid = false;
				try {
					if (this.hwnd == IntPtr.Zero)
						return false;

					Process process = this.OwningProcess;
					if (process == null)
						return false;

					if (WindowInfo.processIDToValidityMap.TryGetValue(process.Id, out isValid))
						return isValid;

					if (process.Id == Process.GetCurrentProcess().Id)
						isValid = false;
					else if (process.MainWindowHandle == IntPtr.Zero)
						isValid = false;
					// Remove VS, since it loads the WPF assemblies but we know that it doesn't work.
					else if (process.ProcessName.Contains("devenv"))
						isValid = false;
					else {
						foreach (ProcessModule module in process.Modules)
						{
							if (module.ModuleName.Contains("PresentationFramework.dll") ||
								module.ModuleName.Contains("PresentationFramework.ni.dll"))
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

		public Process OwningProcess {
			get { return NativeMethods.GetWindowThreadProcess(this.hwnd); }
		}

		public IntPtr HWnd {
			get { return this.hwnd; }
		}

		public string Description {
			get {
				Process process = this.OwningProcess;
				return process.MainWindowTitle + " - " + process.ProcessName + " [" + process.Id.ToString() + "]";
			}
		}

		public override string ToString() {
			return this.Description;
		}

		public void Snoop() {
			Mouse.OverrideCursor = Cursors.Wait;
			try {
				Injector.Launch(this.HWnd, typeof(SnoopUI).Assembly, typeof(SnoopUI).FullName, "GoBabyGo");
			}
			catch (Exception) {
				this.appChooser.Refresh();
			}
			Mouse.OverrideCursor = null;
		}

		public void Magnify() {
			Mouse.OverrideCursor = Cursors.Wait;
			try {
				Injector.Launch(this.HWnd, typeof(Zoomer).Assembly, typeof(Zoomer).FullName, "GoBabyGo");
			}
			catch (Exception) {
				this.appChooser.Refresh(); 
			}
			Mouse.OverrideCursor = null;
		}
	}
}
