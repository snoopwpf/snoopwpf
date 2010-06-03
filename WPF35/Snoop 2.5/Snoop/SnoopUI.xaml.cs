// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Forms.Integration;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;

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
		#endregion

		#region Static Constructor
		static SnoopUI()
		{
			SnoopUI.IntrospectCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
			SnoopUI.RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
			SnoopUI.HelpCommand.InputGestures.Add(new KeyGesture(Key.F1));
		}
		#endregion

		#region Public Constructor
		public SnoopUI()
		{
			this.filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

			this.InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;
			this.InitializeComponent();

			PresentationTraceSources.Refresh();
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

			this.CommandBindings.Add(new CommandBinding(SnoopUI.IntrospectCommand, this.HandleIntrospection));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.RefreshCommand, this.HandleRefresh));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.HelpCommand, this.HandleHelp));

			// cplotts todo: how does this inspect command work? seems tied into the events view.
			this.CommandBindings.Add(new CommandBinding(SnoopUI.InspectCommand, this.HandleInspect));

			this.CommandBindings.Add(new CommandBinding(SnoopUI.SelectFocusCommand, this.HandleSelectFocus));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.SelectFocusScopeCommand, this.HandleSelectFocusScope));

			InputManager.Current.PreProcessInput += this.HandlePreProcessInput;
			this.Tree.SelectedItemChanged += this.HandleTreeSelectedItemChanged;
		}
		#endregion

		#region Public Static Methods
		private delegate void Action();

		public static void GoBabyGo()
		{
			Dispatcher dispatcher;
			if (Application.Current == null)
				dispatcher = Dispatcher.CurrentDispatcher;
			else
				dispatcher = Application.Current.Dispatcher;

			if (dispatcher.CheckAccess())
			{
				object root = null;

				if (Application.Current != null)
				{
					root = Application.Current;
				}
				else
				{
					// if we don't have a current application,
					// then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).

					// in this case, let's iterate over PresentationSource.CurrentSources,
					// and use the first non-null RootVisual we find as root to inspect.

					foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
					{
						if (presentationSource.RootVisual != null)
						{
							root = presentationSource.RootVisual;
							break;
						}
					}
				}

				if (root != null)
				{
					SnoopUI snoop = new SnoopUI();
					snoop.Inspect(root);
				}
				else
				{
					MessageBox.Show
					(
						"Can't find a current application or a PresentationSource root visual!",
						"Can't Snoop",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
				}
			}
			else
			{
				dispatcher.Invoke((Action)GoBabyGo);
			}
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Root element of the visual tree
		/// </summary>
		public VisualTreeItem Root
		{
			get { return this.rootVisualTreeItem; }
		}
		/// <summary>
		/// rootVisualTreeItem is the VisualTreeItem for the root you are inspecting.
		/// </summary>
		private VisualTreeItem rootVisualTreeItem;
		/// <summary>
		/// root is the object you are inspecting.
		/// </summary>
		private object root;


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
						this.currentSelection.IsSelected = false;

					this.currentSelection = value;

					if (this.currentSelection != null)
						this.currentSelection.IsSelected = true;

					this.OnPropertyChanged("CurrentSelection");
					this.OnPropertyChanged("CurrentFocusScope");

					if (this.filtered.Count > 1 || this.filtered.Count == 1 && this.filtered[0] != this.rootVisualTreeItem)
					{
						// Check whether the selected item is filtered out by the filter,
						// in which case reset the filter.
						VisualTreeItem tmp = this.currentSelection;
						while (tmp != null && !this.filtered.Contains(tmp))
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


		/// <summary>
		/// Pluggable interface for additional VisualTree filters.
		/// Used to enable searching for Sparkle automation IDs.
		/// </summary>
		public Predicate<VisualTreeItem> AdditionalFilter
		{
			get { return this.externalFilter; }
			set { this.externalFilter = value; }
		}
		private Predicate<VisualTreeItem> externalFilter;

		public ObservableCollection<VisualTreeItem> Filtered
		{
			get { return this.filtered; }
		}

		public string Filter
		{
			get { return this.filter; }
			set
			{
				this.filter = value;

				this.filterCall.Enqueue();

				this.OnPropertyChanged("Filter");
			}
		}

		public string EventFilter
		{
			get { return this.eventFilter; }
			set
			{
				this.eventFilter = value;
				EventsListener.Filter = value;
			}
		}

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

		#region Public Methods
		public void Inspect(object root)
		{
			this.root = root;

			this.Load(root);
			this.CurrentSelection = this.rootVisualTreeItem;

			this.OnPropertyChanged("Root");

			if (Application.Current != null)
			{
				// search for a visible window to own the main Snoop UI window
				Window owningWindow = Application.Current.MainWindow;
				if (owningWindow == null || owningWindow.Visibility != Visibility.Visible)
				{
					foreach (Window window in Application.Current.Windows)
					{
						if (window.Visibility == Visibility.Visible)
						{
							owningWindow = window;
							break;
						}
					}
				}

				if (owningWindow != null && owningWindow.Visibility == Visibility.Visible)
				{
					this.Owner = owningWindow;
				}
				else
				{
					MessageBox.Show
					(
						"Can't find a visible window to own Snoop",
						"Can't Snoop",
						MessageBoxButton.OK,
						MessageBoxImage.Exclamation
					);
				}
			}
			else
			{
				// if we don't have a current application,
				// then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).
	
				if (System.Windows.Forms.Application.OpenForms.Count > 0)
				{
					// this is windows forms -> wpf interop

					// call ElementHost.EnableModelessKeyboardInterop to allow the Snoop UI window
					// to receive keyboard messages. if you don't call this method,
					// you will be unable to edit properties in the property grid for windows forms interop.
					ElementHost.EnableModelessKeyboardInterop(this);
				}
			}
			this.Show();
			this.Activate();
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
							this.filtered.Clear();
							this.filtered.Add(m_reducedDepthRoot);
							m_reducedDepthRoot = null;
						}
					);
				}
				m_reducedDepthRoot = newRoot;
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

			this.CurrentSelection = null;

			InputManager.Current.PreProcessInput -= this.HandlePreProcessInput;
			EventsListener.Stop();


			// persist the window placement details to the user settings.
			WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
			IntPtr hwnd = new WindowInteropHelper(this).Handle;
			Win32.GetWindowPlacement(hwnd, out wp);
			Properties.Settings.Default.SnoopUIWindowPlacement = wp;
			Properties.Settings.Default.Save();
		}
		#endregion

		#region Private Routed Event Handlers
		/// <summary>
		/// Just for fun, the ability to run Snoop on itself :)
		/// </summary>
		private void HandleIntrospection(object sender, ExecutedRoutedEventArgs e)
		{
			this.Inspect(this);
		}
		private void HandleRefresh(object sender, ExecutedRoutedEventArgs e)
		{
			Cursor saveCursor = Mouse.OverrideCursor;
			Mouse.OverrideCursor = Cursors.Wait;
			try
			{
				object currentTarget = this.CurrentSelection != null ? this.CurrentSelection.Target : null;

				this.filtered.Clear();

				this.rootVisualTreeItem = VisualTreeItem.Construct(this.root, null);

				// cplotts todo: is this reload really necessary?
				this.rootVisualTreeItem.Reload();
				this.rootVisualTreeItem.UpdateVisualChildrenCount();

				if (currentTarget != null)
				{
					VisualTreeItem visualItem = this.FindItem(currentTarget);
					if (visualItem != null)
						this.CurrentSelection = visualItem;
				}

				this.Filter = this.filter;
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
				// cplotts todo:
				// if i click on the event, versus the visual it is routing through (in the events view),
				// then i get here, and the property grid seems stuck forever on it.
				// hmmm. does it make sense to delete this else statment?
				this.PropertyGrid.RootTarget = e.Parameter;
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
		#endregion

		#region Private Methods
		private void ProcessFilter()
		{
			this.filtered.Clear();

			// Blech.
			if (this.filter == "Clear Filter")
			{
				Dispatcher dispatcher = null;
				if (Application.Current == null)
				{
					dispatcher = Dispatcher.CurrentDispatcher;
				}
				else
				{
					dispatcher = Application.Current.Dispatcher;
				}

				dispatcher.BeginInvoke
				(
					DispatcherPriority.Loaded,
					new DispatcherOperationCallback
					(
						delegate(object arg)
						{
							this.Filter = string.Empty;
							return null;
						}
					),
					null
				);
				return;
			}
			else if (this.filter == "Show only visuals with binding errors")
			{
				this.FilterBindings(this.rootVisualTreeItem);
			}
			else if (this.filter.Length == 0)
			{
				this.filtered.Add(this.rootVisualTreeItem);
			}
			else
			{
				this.FilterTree(this.rootVisualTreeItem, this.filter.ToLower());
			}
		}

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

						this.rootVisualTreeItem = new VisualItem(presentationSource.RootVisual, null);
					}
				}

				this.rootVisualTreeItem.Reload();
				this.rootVisualTreeItem.UpdateVisualChildrenCount();
				node = this.rootVisualTreeItem.FindNode(target);

				this.Filter = this.filter;
			}
			return node;
		}

		private void HandleTreeSelectedItemChanged(object sender, EventArgs e)
		{
			VisualTreeItem item = this.Tree.SelectedItem as VisualTreeItem;
			if (item != null)
				this.CurrentSelection = item;
		}

		private void FilterTree(VisualTreeItem node, string filter)
		{
			foreach (VisualTreeItem child in node.Children)
			{
				if (child.Filter(filter) || (this.externalFilter != null && this.externalFilter(child)))
					this.filtered.Add(child);
				else
					FilterTree(child, filter);
			}
		}

		private void FilterBindings(VisualTreeItem node)
		{
			foreach (VisualTreeItem child in node.Children)
			{
				if (child.HasBindingError)
					this.filtered.Add(child);
				else
					FilterBindings(child);
			}
		}

		private void Load(object root)
		{
			this.filtered.Clear();

			this.rootVisualTreeItem = VisualTreeItem.Construct(root, null);

			// cplotts todo: is this reload really necessary?
			this.rootVisualTreeItem.Reload();
			this.rootVisualTreeItem.UpdateVisualChildrenCount();

			this.Filter = this.filter;
		}
		#endregion

		#region Private Fields
		private ObservableCollection<VisualTreeItem> filtered = new ObservableCollection<VisualTreeItem>();

		private string filter = string.Empty;
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
}
