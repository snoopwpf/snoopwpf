namespace Snoop
{
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

	public partial class SnoopUI : INotifyPropertyChanged
	{
		public static readonly RoutedCommand IntrospectCommand = new RoutedCommand("Introspect", typeof(SnoopUI));
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand("Refresh", typeof(SnoopUI));
		public static readonly RoutedCommand HelpCommand = new RoutedCommand("Help", typeof(SnoopUI));
		public static readonly RoutedCommand InspectCommand = new RoutedCommand("Inspect", typeof(SnoopUI));
		public static readonly RoutedCommand SelectFocusCommand = new RoutedCommand("SelectFocus", typeof(SnoopUI));

		private VisualItem root;
		private Visual rootVisual;
		private ObservableCollection<VisualTreeItem> filtered = new ObservableCollection<VisualTreeItem>();

		private string filter = string.Empty;

		private string propertyFilter = string.Empty;
		private string eventFilter = string.Empty;

		private VisualTreeItem currentSelection = null;
		private Predicate<VisualTreeItem> externalFilter;
		private DelayedCall filterCall;

		static SnoopUI() {
			SnoopUI.IntrospectCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
			SnoopUI.RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
			SnoopUI.HelpCommand.InputGestures.Add(new KeyGesture(Key.F1));
		}

		public SnoopUI() {
			this.filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

			this.InitializeComponent();

			this.CommandBindings.Add(new CommandBinding(SnoopUI.IntrospectCommand, this.HandleIntrospection));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.RefreshCommand, this.HandleRefresh));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.HelpCommand, this.HandleHelp));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.InspectCommand, this.HandleInspect));
			this.CommandBindings.Add(new CommandBinding(SnoopUI.SelectFocusCommand, this.HandleSelectFocus));

			InputManager.Current.PreProcessInput += this.HandlePreProcessInput;
			this.Tree.SelectedItemChanged += this.HandleTreeSelectedItemChanged;
		}

		/// <summary>
		/// Pluggable interface for additional VisualTree filters. 
		/// Used to enable searching for Sparkle automation IDs.
		/// </summary>
		public Predicate<VisualTreeItem> AdditionalFilter {
			get { return this.externalFilter; }
			set { this.externalFilter = value; }
		}

		/// <summary>
		/// Root element of the visual tree
		/// </summary>
		public VisualTreeItem Root {
			get { return this.root; }
		}

		public void Inspect(Visual visual) {
			this.rootVisual = visual;
			this.Load(visual);
			this.CurrentSelection = this.root;

			this.OnPropertyChanged("Root");

			this.Owner = Application.Current.MainWindow;
			this.Show();
		}

		// Cleanup when closing the window.
		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);

			this.CurrentSelection = null;
			InputManager.Current.PreProcessInput -= this.HandlePreProcessInput;
			EventsListener.Stop();
		}

		public ObservableCollection<VisualTreeItem> Filtered {
			get { return this.filtered; }
		}

		public string Filter {
			get { return this.filter; }
			set {
				this.filter = value;

				this.filterCall.Enqueue();

				this.OnPropertyChanged("Filter");
			}
		}

		private void ProcessFilter()
		{
			this.filtered.Clear();

			// Blech.
			if (this.filter == "Clear Filter")
			{
				Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(
				delegate(object arg)
				{
					this.Filter = string.Empty;
					return null;
				}), null);
				return;
			}
			if (this.filter == "Visuals with binding Errors")
				this.FilterBindings(this.root);
			else if (this.filter.Length == 0)
				this.filtered.Add(this.root);
			else
				this.FilterTree(this.root, this.filter.ToLower());
		}

		public string EventFilter {
			get { return this.eventFilter; }
			set {
				this.eventFilter = value;
				EventsListener.Filter = value;
			}
		}

		/// <summary>
		/// Currently selected item in the tree view.
		/// </summary>
		public VisualTreeItem CurrentSelection {
			get { return this.currentSelection; }
			set {
				if (this.currentSelection != value) {
					if (this.currentSelection != null)
						this.currentSelection.IsSelected = false;

					this.currentSelection = value;

					if (this.currentSelection != null)
						this.currentSelection.IsSelected = true;

					this.OnPropertyChanged("CurrentSelection");
				}
			}
		}

		public IInputElement CurrentFocus {
			get { return Keyboard.FocusedElement; }
		}

		private void HandlePreProcessInput(object sender, PreProcessInputEventArgs e) {
			//KeyboardFocusChangedEventArgs keyboardFocusChange = e.PeekInput().Input as KeyboardFocusChangedEventArgs;
			//if (keyboardFocusChange != null) {
				this.OnPropertyChanged("CurrentFocus");
			//}

			KeyboardDevice keyboard = System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice;
			ModifierKeys currentModifiers = InputManager.Current.PrimaryKeyboardDevice.Modifiers;
			if (!(
				(currentModifiers & ModifierKeys.Control) != 0 &&
				(currentModifiers & ModifierKeys.Shift) != 0))
			{
				return;
			}

			Visual directlyOver = Mouse.PrimaryDevice.DirectlyOver as Visual;
			if ((directlyOver == null) || directlyOver.IsDescendantOf(this))
				return;

			VisualTreeItem node = this.FindItem(directlyOver);
			if (node != null)
				this.CurrentSelection = node;
		}

		/// <summary>
		/// Just for fun, the ability to run Snoop on itself :)
		/// </summary>
		private void HandleIntrospection(object sender, ExecutedRoutedEventArgs e) {
			this.Inspect(this);
		}

		private void HandleRefresh(object sender, ExecutedRoutedEventArgs e) {
			DependencyObject currentTarget = this.CurrentSelection != null ? this.CurrentSelection.Target : null;

			this.filtered.Clear();

			this.root = new VisualItem(this.rootVisual, null);

			this.root.Reload();
			this.root.UpdateChildrenCount();

			if (currentTarget != null) {
				VisualTreeItem visualItem = this.FindItem(currentTarget);
				if (visualItem != null)
					this.CurrentSelection = visualItem;
			}

			this.Filter = this.filter;
		}

		private void HandleHelp(object sender, ExecutedRoutedEventArgs e) {
			//Help help = new Help();
			//help.Show();
		}

		private void HandleInspect(object sender, ExecutedRoutedEventArgs e) {

			Visual visual = e.Parameter as Visual;
			if (visual != null) {
				VisualTreeItem node = this.FindItem(visual);
				if (node != null)
					this.CurrentSelection = node;
			}
			else if (e.Parameter != null)
				this.PropertyGrid.SetTarget(e.Parameter);
		}

		private void HandleSelectFocus(object sender, ExecutedRoutedEventArgs e) {
			DependencyObject target = e.Parameter as DependencyObject;
			if (target != null) {
				VisualTreeItem node = this.FindItem(target);
				if (node != null)
					this.CurrentSelection = node;
			}
		}

		/// Find the VisualTreeItem for the specified visual.
		/// If the item is not found and is not part of the Snoop UI, the tree will be adjusted
		/// to include the window the item is in.
		private VisualTreeItem FindItem(DependencyObject target) {

			VisualTreeItem node = this.root.FindNode(target);
			if (node == null) {
				Visual visual = target as Visual;
				if (visual != null) {
					// If not in the root tree, make the root be the tree the visual is in.
					if (!visual.IsDescendantOf(this.root.Visual))
						this.root = new VisualItem(PresentationSource.FromVisual(visual).RootVisual, null);
				}

				this.root.Reload();
				this.root.UpdateChildrenCount();
				node = this.root.FindNode(target);

				this.Filter = this.filter;
			}
			return node;
		}

		private void HandleTreeSelectedItemChanged(object sender, EventArgs e) {
			VisualTreeItem item = this.Tree.SelectedItem as VisualTreeItem;
			if (item != null)
				this.CurrentSelection = item;
		}

		private void FilterTree(VisualTreeItem node, string filter) {
			foreach (VisualTreeItem child in node.Children) {
				if (child.Filter(filter) || (this.externalFilter != null && this.externalFilter(child)))
					this.filtered.Add(child);
				else
					FilterTree(child, filter);
			}
		}

		private void FilterBindings(VisualTreeItem node) {
			foreach (VisualTreeItem child in node.Children) {
				if (child.HasBindingError)
					this.filtered.Add(child);
				else
					FilterBindings(child);
			}
		}

		private void Load(Visual rootVisual) {
			this.filtered.Clear();

			this.root = new VisualItem(rootVisual, null);
			this.root.Reload();

			this.root.UpdateChildrenCount();
			this.Filter = this.filter;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public static void GoBabyGo()
		{
			if (Application.Current != null && Application.Current.MainWindow != null)
			{
				SnoopUI snoops = new SnoopUI();
				snoops.Inspect(Application.Current.MainWindow);
			}
		}
	}

	public class NoFocusHyperlink : Hyperlink {
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
			this.OnClick();
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			e.Handled = true;
		}
	}
}
