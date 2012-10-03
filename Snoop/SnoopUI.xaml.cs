// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Forms.Integration;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using Snoop.Infrastructure;
using Snoop.Shell;

namespace Snoop
{
	#region SnoopUI
	public partial class SnoopUI : INotifyPropertyChanged
	{
		#region Public Static Routed Commands
		public static readonly RoutedCommand IntrospectCommand = new RoutedCommand("Introspect", typeof(SnoopUI));
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand("Refresh", typeof(SnoopUI));
		public static readonly RoutedCommand HelpCommand = new RoutedCommand("Help", typeof(SnoopUI));
		public static readonly RoutedCommand InspectCommand = new RoutedCommand("Inspect", typeof(SnoopUI));
		public static readonly RoutedCommand SelectFocusCommand = new RoutedCommand("SelectFocus", typeof(SnoopUI));
		public static readonly RoutedCommand SelectFocusScopeCommand = new RoutedCommand("SelectFocusScope", typeof(SnoopUI));
		public static readonly RoutedCommand ClearSearchFilterCommand = new RoutedCommand("ClearSearchFilter", typeof(SnoopUI));
		public static readonly RoutedCommand CopyPropertyChangesCommand = new RoutedCommand("CopyPropertyChanges", typeof(SnoopUI));
		#endregion

		#region Static Constructor
		static SnoopUI()
		{
			SnoopUI.IntrospectCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
			SnoopUI.RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
			SnoopUI.HelpCommand.InputGestures.Add(new KeyGesture(Key.F1));
			SnoopUI.ClearSearchFilterCommand.InputGestures.Add(new KeyGesture(Key.Escape));
			SnoopUI.CopyPropertyChangesCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
		}
		#endregion

		#region Public Constructor
		public SnoopUI()
		{
			this.filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

			this.InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;
			this.InitializeComponent();

			// wrap the following PresentationTraceSources.Refresh() call in a try/catch
			// sometimes a NullReferenceException occurs
			// due to empty <filter> elements in the app.config file of the app you are snooping
			// see the following for more info:
			// http://snoopwpf.codeplex.com/discussions/236503
			// http://snoopwpf.codeplex.com/workitem/6647
			try
			{
				PresentationTraceSources.Refresh();
				PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
			}
			catch (NullReferenceException)
			{
				// swallow this exception since you can Snoop just fine anyways.
			}

			this.CommandBindings.Add(new CommandBinding(SnoopUI.IntrospectCommand, this.HandleIntrospection));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.RefreshCommand, this.HandleRefresh));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.HelpCommand, this.HandleHelp));

			this.CommandBindings.Add(new CommandBinding(SnoopUI.InspectCommand, this.HandleInspect));

			this.CommandBindings.Add(new CommandBinding(SnoopUI.SelectFocusCommand, this.HandleSelectFocus));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.SelectFocusScopeCommand, this.HandleSelectFocusScope));

			//NOTE: this is up here in the outer UI layer so ESC will clear any typed filter regardless of where the focus is
			// (i.e. focus on a selected item in the tree, not in the property list where the search box is hosted)
			this.CommandBindings.Add(new CommandBinding(SnoopUI.ClearSearchFilterCommand, this.ClearSearchFilterHandler));

			this.CommandBindings.Add(new CommandBinding(SnoopUI.CopyPropertyChangesCommand, this.CopyPropertyChangesHandler));

			InputManager.Current.PreProcessInput += this.HandlePreProcessInput;
			this.Tree.SelectedItemChanged += this.HandleTreeSelectedItemChanged;

			// we can't catch the mouse wheel at the ZoomerControl level,
			// so we catch it here, and relay it to the ZoomerControl.
			this.MouseWheel += this.SnoopUI_MouseWheel;

			filterTimer = new DispatcherTimer();
			filterTimer.Interval = TimeSpan.FromSeconds(0.3);
			filterTimer.Tick += (s, e) =>
			{
				EnqueueAfterSettingFilter();
				filterTimer.Stop();
			};

		    InitShell();
		}

        private void InitShell()
        {
            if (ShellConstants.IsPowerShellInstalled)
            {
                var shell = new EmbeddedShellView();
                shell.Start(this);

                this.PowerShellTab.Content = shell;

                RoutedPropertyChangedEventHandler<object> onSelectedItemChanged = (sender, e) => shell.NotifySelected(this.CurrentSelection);
                Action<VisualTreeItem> onProviderLocationChanged = item => this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    item.IsSelected = true;
                    this.CurrentSelection = item;
                }));

                // sync the current location
                this.Tree.SelectedItemChanged += onSelectedItemChanged;
                shell.ProviderLocationChanged += onProviderLocationChanged;

                // clean up garbage!
                this.Closed += delegate
                {
                    this.Tree.SelectedItemChanged -= onSelectedItemChanged;
                    shell.ProviderLocationChanged -= onProviderLocationChanged;
                };
            }
        }

	    #endregion

		#region Public Static Methods
		public static bool GoBabyGo()
		{
			try
			{
				SnoopApplication();
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("There was an error snooping! Message = {0}\n\nStack Trace:\n{1}", ex.Message, ex.StackTrace), "Error Snooping", MessageBoxButton.OK);
				return false;
			}
		}

		public static void SnoopApplication()
		{
			Dispatcher dispatcher;
			if (Application.Current == null)
				dispatcher = Dispatcher.CurrentDispatcher;
			else
				dispatcher = Application.Current.Dispatcher;

			if (dispatcher.CheckAccess())
			{
				SnoopUI snoop = new SnoopUI();
				var title = TryGetMainWindowTitle();
				if (!string.IsNullOrEmpty(title))
				{
					snoop.Title = string.Format("{0} - Snoop", title);
				}

				snoop.Inspect();

				CheckForOtherDispatchers(dispatcher);
			}
			else
			{
				dispatcher.Invoke((Action)SnoopApplication);
				return;
			}
		}

		private static void CheckForOtherDispatchers(Dispatcher mainDispatcher)
		{
			// check and see if any of the root visuals have a different mainDispatcher
			// if so, ask the user if they wish to enter multiple mainDispatcher mode.
			// if they do, launch a snoop ui for every additional mainDispatcher.
			// see http://snoopwpf.codeplex.com/workitem/6334 for more info.

			List<Visual> rootVisuals = new List<Visual>();
			List<Dispatcher> dispatchers = new List<Dispatcher>();
			dispatchers.Add(mainDispatcher);
			foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
			{
				Visual presentationSourceRootVisual = presentationSource.RootVisual;

                if (!(presentationSourceRootVisual is Window))
					continue;

				Dispatcher presentationSourceRootVisualDispatcher = presentationSourceRootVisual.Dispatcher;

				if (dispatchers.IndexOf(presentationSourceRootVisualDispatcher) == -1)
				{
					rootVisuals.Add(presentationSourceRootVisual);
					dispatchers.Add(presentationSourceRootVisualDispatcher);
				}
			}

			if (rootVisuals.Count > 0)
			{
				var result =
					MessageBox.Show
					(
						"Snoop has noticed windows running in multiple dispatchers!\n\n" +
						"Would you like to enter multiple dispatcher mode, and have a separate Snoop window for each dispatcher?\n\n" +
						"Without having a separate Snoop window for each dispatcher, you will not be able to Snoop the windows in the dispatcher threads outside of the main dispatcher. " +
						"Also, note, that if you bring up additional windows in additional dispatchers (after Snooping), you will need to Snoop again in order to launch Snoop windows for those additional dispatchers.",
						"Enter Multiple Dispatcher Mode",
						MessageBoxButton.YesNo,
						MessageBoxImage.Question
					);

				if (result == MessageBoxResult.Yes)
				{
					SnoopModes.MultipleDispatcherMode = true;
					Thread thread = new Thread(new ParameterizedThreadStart(DispatchOut));
					thread.Start(rootVisuals);
				}
			}
		}

		private static void DispatchOut(object o)
		{
			List<Visual> visuals = (List<Visual>)o;
			foreach (var v in visuals)
			{
				// launch a snoop ui on each dispatcher
				v.Dispatcher.Invoke
				(
					(Action)
					(
						() =>
						{
							SnoopUI snoopOtherDispatcher = new SnoopUI();
							snoopOtherDispatcher.Inspect(v, v as Window);
						}
					)
				);
			}
		}
		private delegate void Action();
		#endregion

		#region Public Properties
		#region VisualTreeItems
		/// <summary>
		/// This is the collection of VisualTreeItem(s) that the visual tree TreeView binds to.
		/// </summary>
		public ObservableCollection<VisualTreeItem> VisualTreeItems
		{
			get { return this.visualTreeItems; }
		}
		#endregion

		#region Root

	    /// <summary>
	    /// Root element of the visual tree
	    /// </summary>
	    public VisualTreeItem Root
	    {
	        get { return this.rootVisualTreeItem; }
            private set
            {
                this.rootVisualTreeItem = value;
                this.OnPropertyChanged("Root");
            }
	    }

	    /// <summary>
		/// rootVisualTreeItem is the VisualTreeItem for the root you are inspecting.
		/// </summary>
		private VisualTreeItem rootVisualTreeItem;
		/// <summary>
		/// root is the object you are inspecting.
		/// </summary>
		private object root;
		#endregion

		#region CurrentSelection
		/// <summary>
		/// Currently selected item in the tree view.
		/// </summary>
		public VisualTreeItem CurrentSelection
		{
			get { return this.currentSelection; }
			set
			{
				if (this.currentSelection != value)
				{
					if (this.currentSelection != null)
					{
						SaveEditedProperties(currentSelection);
						this.currentSelection.IsSelected = false;
					}

					this.currentSelection = value;

					if (this.currentSelection != null)
					{
						this.currentSelection.IsSelected = true;
						_lastNonNullSelection = currentSelection;
					}

					this.OnPropertyChanged("CurrentSelection");
					this.OnPropertyChanged("CurrentFocusScope");

					if (this.visualTreeItems.Count > 1 || this.visualTreeItems.Count == 1 && this.visualTreeItems[0] != this.rootVisualTreeItem)
					{
						// Check whether the selected item is filtered out by the filter,
						// in which case reset the filter.
						VisualTreeItem tmp = this.currentSelection;
						while (tmp != null && !this.visualTreeItems.Contains(tmp))
						{
							tmp = tmp.Parent;
						}
						if (tmp == null)
						{
							// The selected item is not a descendant of any root.
							RefreshCommand.Execute(null, this);
						}
					}
				}
			}
		}

		private VisualTreeItem currentSelection = null;
		private VisualTreeItem _lastNonNullSelection = null;

		#endregion

		#region Filter
		/// <summary>
		/// This Filter property is bound to the editable combo box that the user can type in to filter the visual tree TreeView.
		/// Every time the user types a key, the setter gets called, enqueueing a delayed call to the ProcessFilter method.
		/// </summary>
		public string Filter
		{
			get { return this.filter; }
			set
			{
				this.filter = value;

				if (!fromTextBox)
				{
					EnqueueAfterSettingFilter();
				}
				else
				{
					filterTimer.Stop();
					filterTimer.Start();
				}
			}
		}

		private void SetFilter(string value)
		{
			fromTextBox = false;
			this.Filter = value;
			fromTextBox = true;
		}

		private void EnqueueAfterSettingFilter()
		{
			this.filterCall.Enqueue();

			this.OnPropertyChanged("Filter");
		}

		private string filter = string.Empty;
		#endregion

		#region EventFilter
		public string EventFilter
		{
			get { return this.eventFilter; }
			set
			{
				this.eventFilter = value;
				EventsListener.Filter = value;
			}
		}
		#endregion

		#region CurrentFocus
		public IInputElement CurrentFocus
		{
			get
			{
				var newFocus = Keyboard.FocusedElement;
				if (newFocus != this.currentFocus)
				{
					// Store reference to previously focused element only if focused element was changed.
					this.previousFocus = this.currentFocus;
				}
				this.currentFocus = newFocus;

				return this.returnPreviousFocus ? this.previousFocus : this.currentFocus;
			}
		}
		#endregion

		#region CurrentFocusScope
		public object CurrentFocusScope
		{
			get
			{
				if (CurrentSelection == null)
					return null;

				var selectedItem = CurrentSelection.Target as DependencyObject;
				if (selectedItem != null)
				{
					return FocusManager.GetFocusScope(selectedItem);
				}
				return null;
			}
		}
		#endregion
		#endregion

		#region Public Methods
		public void Inspect()
		{
			object root = FindRoot();
			if (root == null)
			{
				if (!SnoopModes.MultipleDispatcherMode)
				{
					//SnoopModes.MultipleDispatcherMode is always false for all scenarios except for cases where we are running multiple dispatchers.
					//If SnoopModes.MultipleDispatcherMode was set to true, then there definitely was a root visual found in another dispatcher, so
					//the message below would be wrong.
					MessageBox.Show
					(
						"Can't find a current application or a PresentationSource root visual!",
						"Can't Snoop",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
				}

				return;
			}
			Load(root);

			Window ownerWindow = SnoopWindowUtils.FindOwnerWindow();
			if (ownerWindow != null)
			{
				if (ownerWindow.Dispatcher != this.Dispatcher)
				{
					return;
				}
				this.Owner = ownerWindow;

				// watch for window closing so we can spit out the changed properties
				ownerWindow.Closing += SnoopedWindowClosingHandler;
			}

			SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);
			this.Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(UnhandledExceptionHandler);

			Show();
			Activate();
		}
		public void Inspect(object root, Window ownerWindow)
		{
			this.Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(UnhandledExceptionHandler);

			Load(root);

			if (ownerWindow != null)
			{
				this.Owner = ownerWindow;

				// watch for window closing so we can spit out the changed properties
				ownerWindow.Closing += SnoopedWindowClosingHandler;
			}

			SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);

			Show();
			Activate();
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

			// should we check if the exception came from Snoop? perhaps seeing if any Snoop call is in the stack trace?
			ErrorDialog dialog = new ErrorDialog();
			dialog.Exception = e.Exception;
			var result = dialog.ShowDialog();
			if (result.HasValue && result.Value)
				e.Handled = true;
		}

		public void ApplyReduceDepthFilter(VisualTreeItem newRoot)
		{
			if (m_reducedDepthRoot != newRoot)
			{
				if (m_reducedDepthRoot == null)
				{
					Dispatcher.BeginInvoke
					(
						DispatcherPriority.Background,
						(function)
						delegate
						{
							this.visualTreeItems.Clear();
							this.visualTreeItems.Add(m_reducedDepthRoot);
							m_reducedDepthRoot = null;
						}
					);
				}
				m_reducedDepthRoot = newRoot;
			}
		}


		/// <summary>
		/// Loop through the properties in the current PropertyGrid and save away any properties
		/// that have been changed by the user.  
		/// </summary>
		/// <param name="owningObject">currently selected object that owns the properties in the grid (before changing selection to the new object)</param>
		private void SaveEditedProperties( VisualTreeItem owningObject )
		{
            foreach (PropertyInformation property in PropertyGrid.PropertyGrid.Properties)
            {
                if (property.IsValueChangedByUser)
                {
                    EditedPropertiesHelper.AddEditedProperty( Dispatcher, owningObject, property );
                }
            }
		}

		#endregion

		#region Protected Event Overrides
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			try
			{
				// load the window placement details from the user settings.
				WINDOWPLACEMENT wp = (WINDOWPLACEMENT)Properties.Settings.Default.SnoopUIWindowPlacement;
				wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
				wp.flags = 0;
				wp.showCmd = (wp.showCmd == Win32.SW_SHOWMINIMIZED ? Win32.SW_SHOWNORMAL : wp.showCmd);
				IntPtr hwnd = new WindowInteropHelper(this).Handle;
				Win32.SetWindowPlacement(hwnd, ref wp);

				// load whether all properties are shown by default
				this.PropertyGrid.ShowDefaults = Properties.Settings.Default.ShowDefaults;

				// load whether the previewer is shown by default
				this.PreviewArea.IsActive = Properties.Settings.Default.ShowPreviewer;
			}
			catch
			{
			}
		}
		/// <summary>
		/// Cleanup when closing the window.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			// unsubscribe to owner window closing event
			// replaces previous attempts to hookup to MainWindow.Closing on the wrong dispatcher thread
			// This one should be running on the right dispatcher thread since this SnoopUI instance
			// is wired up to the dispatcher thread/window that it owns
			if ( Owner != null )
			{
				Owner.Closing -= SnoopedWindowClosingHandler;
			}

			this.CurrentSelection = null;

			InputManager.Current.PreProcessInput -= this.HandlePreProcessInput;
			EventsListener.Stop();

			EditedPropertiesHelper.DumpObjectsWithEditedProperties();

			// persist the window placement details to the user settings.
			WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
			IntPtr hwnd = new WindowInteropHelper(this).Handle;
			Win32.GetWindowPlacement(hwnd, out wp);
			Properties.Settings.Default.SnoopUIWindowPlacement = wp;

			// persist whether all properties are shown by default
			Properties.Settings.Default.ShowDefaults = this.PropertyGrid.ShowDefaults;

			// persist whether the previewer is shown by default
			Properties.Settings.Default.ShowPreviewer = this.PreviewArea.IsActive;

			// actually do the persisting
			Properties.Settings.Default.Save();

			SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
		}

		/// <summary>
		/// Event handler for a snooped window closing. This is our chance to spit out 
		/// all the properties that changed during the snoop session for that window.
		/// Note: there may be multiple snooped windows (when in multiple dispatcher mode)
		/// and each window is hooked up to it's own instance of SnoopUI and this event.
		/// </summary>
		private void SnoopedWindowClosingHandler(object sender, CancelEventArgs e)
		{
			// changing the selection captures any changes in the selected item at the time of window closing 
			this.CurrentSelection = null;
		    EditedPropertiesHelper.DumpObjectsWithEditedProperties();
		}

		#endregion

		#region Private Routed Event Handlers
		/// <summary>
		/// Just for fun, the ability to run Snoop on itself :)
		/// </summary>
		private void HandleIntrospection(object sender, ExecutedRoutedEventArgs e)
		{
			this.Load(this);
		}
		private void HandleRefresh(object sender, ExecutedRoutedEventArgs e)
		{
			Cursor saveCursor = Mouse.OverrideCursor;
			Mouse.OverrideCursor = Cursors.Wait;
			try
			{
				object currentTarget = this.CurrentSelection != null ? this.CurrentSelection.Target : null;

				this.visualTreeItems.Clear();

				this.Root = VisualTreeItem.Construct(this.root, null);

				if (currentTarget != null)
				{
					VisualTreeItem visualItem = this.FindItem(currentTarget);
					if (visualItem != null)
						this.CurrentSelection = visualItem;
				}

				this.SetFilter(this.filter);
			}
			finally
			{
				Mouse.OverrideCursor = saveCursor;
			}
		}
		private void HandleHelp(object sender, ExecutedRoutedEventArgs e)
		{
			//Help help = new Help();
			//help.Show();
		}
		private void HandleInspect(object sender, ExecutedRoutedEventArgs e)
		{
			Visual visual = e.Parameter as Visual;
			if (visual != null)
			{
				VisualTreeItem node = this.FindItem(visual);
				if (node != null)
					this.CurrentSelection = node;
			}
			else if (e.Parameter != null)
			{
				this.PropertyGrid.SetTarget(e.Parameter);
			}
		}
		private void HandleSelectFocus(object sender, ExecutedRoutedEventArgs e)
		{
			// We know we've stolen focus here. Let's use previously focused element.
			this.returnPreviousFocus = true;
			SelectItem(CurrentFocus as DependencyObject);
			this.returnPreviousFocus = false;
			OnPropertyChanged("CurrentFocus");
		}

		private void HandleSelectFocusScope(object sender, ExecutedRoutedEventArgs e)
		{
			SelectItem(e.Parameter as DependencyObject);
		}

		private void ClearSearchFilterHandler(object sender, ExecutedRoutedEventArgs e)
		{
			PropertyGrid.StringFilter = string.Empty;
		}

		private void CopyPropertyChangesHandler(object sender, ExecutedRoutedEventArgs e)
		{
			if (this.currentSelection != null)
				SaveEditedProperties(this.currentSelection);

			EditedPropertiesHelper.DumpObjectsWithEditedProperties();
		}

		private void SelectItem(DependencyObject item)
		{
			if (item != null)
			{
				VisualTreeItem node = this.FindItem(item);
				if (node != null)
					this.CurrentSelection = node;
			}
		}
		#endregion

		#region Private Event Handlers
		private void HandlePreProcessInput(object sender, PreProcessInputEventArgs e)
		{
			this.OnPropertyChanged("CurrentFocus");

			ModifierKeys currentModifiers = InputManager.Current.PrimaryKeyboardDevice.Modifiers;
			if (!((currentModifiers & ModifierKeys.Control) != 0 && (currentModifiers & ModifierKeys.Shift) != 0))
				return;

			Visual directlyOver = Mouse.PrimaryDevice.DirectlyOver as Visual;
			if ((directlyOver == null) || directlyOver.IsDescendantOf(this))
				return;

			VisualTreeItem node = this.FindItem(directlyOver);
			if (node != null)
				this.CurrentSelection = node;
		}
		private void SnoopUI_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			this.PreviewArea.Zoomer.DoMouseWheel(sender, e);
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Find the VisualTreeItem for the specified visual.
		/// If the item is not found and is not part of the Snoop UI,
		/// the tree will be adjusted to include the window the item is in.
		/// </summary>
		private VisualTreeItem FindItem(object target)
		{
			VisualTreeItem node = this.rootVisualTreeItem.FindNode(target);
			Visual rootVisual = this.rootVisualTreeItem.MainVisual;
			if (node == null)
			{
				Visual visual = target as Visual;
				if (visual != null && rootVisual != null)
				{
					// If target is a part of the SnoopUI, let's get out of here.
					if (visual.IsDescendantOf(this))
					{
						return null;
					}

					// If not in the root tree, make the root be the tree the visual is in.
					if (!visual.IsDescendantOf(rootVisual))
					{
						var presentationSource = PresentationSource.FromVisual(visual);
						if (presentationSource == null)
						{
							return null; // Something went wrong. At least we will not crash with null ref here.
						}

						this.Root = new VisualItem(presentationSource.RootVisual, null);
					}
				}

				this.rootVisualTreeItem.Reload();

				node = this.rootVisualTreeItem.FindNode(target);

				this.SetFilter(this.filter);
			}
			return node;
		}

		private static string TryGetMainWindowTitle()
		{
			if (Application.Current != null && Application.Current.MainWindow != null)
			{
				return Application.Current.MainWindow.Title;
			}
			return string.Empty;
		}

		private void HandleTreeSelectedItemChanged(object sender, EventArgs e)
		{
			VisualTreeItem item = this.Tree.SelectedItem as VisualTreeItem;
			if (item != null)
				this.CurrentSelection = item;
		}

		private void ProcessFilter()
		{
			if (SnoopModes.MultipleDispatcherMode && !this.Dispatcher.CheckAccess())
			{
				Action action = () => ProcessFilter();
				this.Dispatcher.BeginInvoke(action);
				return;
			}

			this.visualTreeItems.Clear();

			// cplotts todo: we've got to come up with a better way to do this.
			if (this.filter == "Clear any filter applied to the tree view")
			{
				this.SetFilter(string.Empty);
			}
			else if (this.filter == "Show only visuals with binding errors")
			{
				this.FilterBindings(this.rootVisualTreeItem);
			}
			else if (this.filter.Length == 0)
			{
				this.visualTreeItems.Add(this.rootVisualTreeItem);
			}
			else
			{
				this.FilterTree(this.rootVisualTreeItem, this.filter.ToLower());
			}
		}

		private void FilterTree(VisualTreeItem node, string filter)
		{
			foreach (VisualTreeItem child in node.Children)
			{
				if (child.Filter(filter))
					this.visualTreeItems.Add(child);
				else
					FilterTree(child, filter);
			}
		}
		private void FilterBindings(VisualTreeItem node)
		{
			foreach (VisualTreeItem child in node.Children)
			{
				if (child.HasBindingError)
					this.visualTreeItems.Add(child);
				else
					FilterBindings(child);
			}
		}

		private object FindRoot()
		{
			object root = null;

			if (SnoopModes.MultipleDispatcherMode)
			{
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
					if
					(
						presentationSource.RootVisual != null &&
						presentationSource.RootVisual is UIElement &&
						((UIElement)presentationSource.RootVisual).Dispatcher.CheckAccess()
					)
					{
						root = presentationSource.RootVisual;
						break;
					}
				}
			}
			else if (Application.Current != null)
			{
				root = Application.Current;
			}
			else
			{
				// if we don't have a current application,
				// then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).


				// in this case, let's iterate over PresentationSource.CurrentSources,
				// and use the first non-null, visible RootVisual we find as root to inspect.
				foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
				{
					if
					(
						presentationSource.RootVisual != null &&
						presentationSource.RootVisual is UIElement &&
						((UIElement)presentationSource.RootVisual).Visibility == Visibility.Visible
					)
					{
						root = presentationSource.RootVisual;
						break;
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
			}

			return root;
		}

		private void Load(object root)
		{
			this.root = root;

			this.visualTreeItems.Clear();

			this.Root = VisualTreeItem.Construct(root, null);
			this.CurrentSelection = this.rootVisualTreeItem;

			this.SetFilter(this.filter);

			this.OnPropertyChanged("Root");
		}
		#endregion

		#region Private Fields
		private bool fromTextBox = true;
		private DispatcherTimer filterTimer;

		private ObservableCollection<VisualTreeItem> visualTreeItems = new ObservableCollection<VisualTreeItem>();

		private string propertyFilter = string.Empty;
		private string eventFilter = string.Empty;

		private DelayedCall filterCall;

		private VisualTreeItem m_reducedDepthRoot;

		private IInputElement currentFocus;
		private IInputElement previousFocus;

		/// <summary>
		/// Indicates whether CurrentFocus should retur previously focused element.
		/// This fixes problem where Snoop steals the focus from snooped app.
		/// </summary>
		private bool returnPreviousFocus;

		#endregion

		#region Private Delegates
		private delegate void function();
		#endregion

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
	#endregion

	#region NoFocusHyperlink
	public class NoFocusHyperlink : Hyperlink
	{
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			this.OnClick();
		}
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
	}
	#endregion

	public class PropertyValueInfo
	{
		public string PropertyName { get; set; }
		public object PropertyValue { get; set; }
	}

	public class EditedPropertiesHelper
	{
		private static object _lock = new object();

		private static readonly Dictionary<Dispatcher,Dictionary<VisualTreeItem, List<PropertyValueInfo>>> _itemsWithEditedProperties =
			new Dictionary<Dispatcher,Dictionary<VisualTreeItem, List<PropertyValueInfo>>>();

		public static void AddEditedProperty( Dispatcher dispatcher, VisualTreeItem propertyOwner, PropertyInformation propInfo)
		{
			lock ( _lock )
			{
				List<PropertyValueInfo> propInfoList = null;
				Dictionary<VisualTreeItem, List<PropertyValueInfo>> dispatcherList = null;

				// first get the dictionary we're using for the given dispatcher
				if ( !_itemsWithEditedProperties.TryGetValue( dispatcher, out dispatcherList ) )
				{
					dispatcherList = new Dictionary<VisualTreeItem, List<PropertyValueInfo>>();
					_itemsWithEditedProperties.Add( dispatcher, dispatcherList );
				}

				// now get the property info list for the owning object 
				if ( !dispatcherList.TryGetValue( propertyOwner, out propInfoList ) )
				{
					propInfoList = new List<PropertyValueInfo>();
					dispatcherList.Add( propertyOwner, propInfoList );
				}

				// if we already have a property of that name on this object, remove it
				var existingPropInfo = propInfoList.FirstOrDefault( l => l.PropertyName == propInfo.DisplayName );
				if ( existingPropInfo != null )
				{
					propInfoList.Remove( existingPropInfo );
				}

				// finally add the edited property info
				propInfoList.Add( new PropertyValueInfo
				                  {
				                  	PropertyName = propInfo.DisplayName,
				                  	PropertyValue = propInfo.Value,
				                  } );
			}
		}

		public static void DumpObjectsWithEditedProperties()
		{
			if (_itemsWithEditedProperties.Count == 0)
			{
				return;
			}

			var sb = new StringBuilder();
			sb.AppendFormat
			(
				"Snoop dump as of {0}{1}--- OBJECTS WITH EDITED PROPERTIES ---{1}",
				DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
				Environment.NewLine
			);

			int dispatcherCount = 1;

			foreach (KeyValuePair<Dispatcher, Dictionary<VisualTreeItem, List<PropertyValueInfo>>> dispatcherKVP in _itemsWithEditedProperties)
			{
				if ( _itemsWithEditedProperties.Count > 1 )
				{
					sb.AppendFormat( "-- Dispatcher #{0} -- {1}", dispatcherCount++, Environment.NewLine );
				}

				foreach (KeyValuePair<VisualTreeItem, List<PropertyValueInfo>> objectPropertiesKVP in dispatcherKVP.Value )
				{
					sb.AppendFormat("Object: {0}{1}", objectPropertiesKVP.Key, Environment.NewLine); 
					foreach (PropertyValueInfo propInfo in objectPropertiesKVP.Value)
					{
						sb.AppendFormat
						(
							"\tProperty: {0}, New Value: {1}{2}",
							propInfo.PropertyName,
							propInfo.PropertyValue,
							Environment.NewLine
						);
					}
				}

				if ( _itemsWithEditedProperties.Count > 1 )
				{
					sb.AppendLine();
				}
			}

			Debug.WriteLine(sb.ToString());
			Clipboard.SetText(sb.ToString());
		}

		
	}

}
