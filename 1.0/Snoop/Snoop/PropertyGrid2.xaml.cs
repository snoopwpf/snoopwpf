namespace Snoop
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Threading;

	public partial class PropertyGrid2: INotifyPropertyChanged
	{
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(object), typeof(PropertyGrid2), new PropertyMetadata(new PropertyChangedCallback(PropertyGrid2.HandleTargetChanged)));

		public static readonly RoutedCommand PopTargetCommand = new RoutedCommand("PopTarget", typeof(PropertyGrid2));
		public static readonly RoutedCommand DelveCommand = new RoutedCommand();
		public static readonly RoutedCommand DelveBindingCommand = new RoutedCommand();
		public static readonly RoutedCommand DelveBindingExpressionCommand = new RoutedCommand();
		public static readonly RoutedCommand ClearCommand = new RoutedCommand();
		public static readonly RoutedCommand SortCommand = new RoutedCommand();

		private object target;
		private PropertyFilter propertyFilter = new PropertyFilter(string.Empty, true);
		private ObservableCollection<PropertyInformation> properties = new ObservableCollection<PropertyInformation>();
		private List<object> inspectStack = new List<object>();
		private PropertyInformation selection;

		private IEnumerator<PropertyInformation> propertiesToAdd;

		private GridViewColumnHeader lastHeaderClicked = null;
		private ListSortDirection lastDirection = ListSortDirection.Ascending;
		private DelayedCall processIncrementalCall;
		private DelayedCall filterCall;

		public PropertyGrid2() {

			this.processIncrementalCall = new DelayedCall(this.ProcessIncrementalPropertyAdd, DispatcherPriority.Background);
			this.filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

			this.InitializeComponent();

			this.Unloaded += this.HandleUnloaded;

			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.PopTargetCommand, this.HandlePopTarget, this.CanPopTarget));
			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.DelveCommand, this.HandleDelve, this.CanDelve));
			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.DelveBindingCommand, this.HandleDelveBinding, this.CanDelveBinding));
			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.DelveBindingExpressionCommand, this.HandleDelveBindingExpression, this.CanDelveBindingExpression));
			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.ClearCommand, this.HandleClear, this.CanClear));
			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.SortCommand, this.HandleSort));

		}

		public PropertyInformation Selection {
			get { return this.selection; }
			set { 
				this.selection = value;
				this.OnPropertyChanged("Selection");
			}
		}

		public object Target {
			get { return this.GetValue(PropertyGrid2.TargetProperty); }
			set { this.SetValue(PropertyGrid2.TargetProperty, value); }
		}

		public Type Type {
			get {
				if (this.target != null)
					return this.target.GetType();
				return null;
			}
		}

		public void PushTarget(object target) {
			this.inspectStack.Add(this.target);
			this.ChangeTarget(target);
		}

		public void SetTarget(object target) {
			this.inspectStack.Clear();
			this.ChangeTarget(target);
		}

		private void ChangeTarget(object newTarget) {

			if (this.target != newTarget) {
				this.target = newTarget;

				foreach (PropertyInformation property in this.properties)
					property.Teardown();
				this.properties.Clear();

				this.propertiesToAdd = null;
				this.processIncrementalCall.Enqueue();

				this.OnPropertyChanged("Type");
			}
		}

		/// <summary>
		/// Delayed loading of the property inspector to avoid creating the entire list of property
		/// editors immediately after selection. Keeps that app running smooth.
		/// </summary>
		/// <param name="performInitialization"></param>
		/// <returns></returns>
		private void ProcessIncrementalPropertyAdd() {
			int numberToAdd = 10;

			if (this.propertiesToAdd == null) {
				this.propertiesToAdd = PropertyInformation.GetProperties(this.target).GetEnumerator();

				numberToAdd = 0;
			}
			int i = 0;
			for (; i < numberToAdd && this.propertiesToAdd.MoveNext(); ++i) {

				PropertyInformation property = this.propertiesToAdd.Current;
				property.Filter = this.propertyFilter;
				this.properties.Add(property);
			}

			if (i == numberToAdd)
				this.processIncrementalCall.Enqueue();
			else
				this.propertiesToAdd = null;
		}

		private void HandlePopTarget(object sender, ExecutedRoutedEventArgs e)
		{
			if (this.inspectStack.Count > 0) {
				this.ChangeTarget(this.inspectStack[this.inspectStack.Count - 1]);
				this.inspectStack.RemoveAt(this.inspectStack.Count - 1);
			}
		}

		private void CanPopTarget(object sender, CanExecuteRoutedEventArgs e) {
			if (this.inspectStack.Count > 0) {
				e.Handled = true;
				e.CanExecute = true;
			}
		}

		private void HandleDelve(object sender, ExecutedRoutedEventArgs e) {
			this.PushTarget(((PropertyInformation)e.Parameter).Value);
		}

		private void HandleDelveBinding(object sender, ExecutedRoutedEventArgs e) {
			this.PushTarget(((PropertyInformation)e.Parameter).Binding);
		}

		private void HandleDelveBindingExpression(object sender, ExecutedRoutedEventArgs e) {
			this.PushTarget(((PropertyInformation)e.Parameter).BindingExpression);
		}

		private void CanDelve(object sender, CanExecuteRoutedEventArgs e) {
			if (e.Parameter != null && ((PropertyInformation)e.Parameter).Value != null)
				e.CanExecute = true;
			e.Handled = true;
		}

		private void CanDelveBinding(object sender, CanExecuteRoutedEventArgs e) {
			if (e.Parameter != null && ((PropertyInformation)e.Parameter).Binding != null)
				e.CanExecute = true;
			e.Handled = true;
		}

		private void CanDelveBindingExpression(object sender, CanExecuteRoutedEventArgs e) {
			if (e.Parameter != null && ((PropertyInformation)e.Parameter).BindingExpression != null)
				e.CanExecute = true;
			e.Handled = true;
		}

		private void CanClear(object sender, CanExecuteRoutedEventArgs e) {
			if (e.Parameter != null && ((PropertyInformation)e.Parameter).IsLocallySet)
				e.CanExecute = true;
			e.Handled = true;
		}

		private void HandleClear(object sender, ExecutedRoutedEventArgs e) {
			((PropertyInformation)e.Parameter).Clear();
		}


		private void HandleSort(object sender, ExecutedRoutedEventArgs args) {
			GridViewColumnHeader headerClicked = (GridViewColumnHeader)args.OriginalSource;

			if (headerClicked != null) {
				ListSortDirection direction = ListSortDirection.Ascending;

				if (headerClicked == this.lastHeaderClicked && this.lastDirection == ListSortDirection.Ascending)
					direction = ListSortDirection.Descending;

				switch ((string)headerClicked.Column.Header) {
					case "Name":
						this.Sort(PropertyGrid2.CompareNames, direction);
						break;
					case "Value":
						this.Sort(PropertyGrid2.CompareValues, direction);
						break;
					case "ValueSource":
						this.Sort(PropertyGrid2.CompareValueSources, direction);
						break;
				}
				this.lastHeaderClicked = headerClicked;
				this.lastDirection = direction;
			}
		}

		public ObservableCollection<PropertyInformation> Properties {
			get { return this.properties; }
		}

		public string StringFilter {
			get { return this.propertyFilter.FilterString; }
			set {
				this.propertyFilter.FilterString = value;

				this.filterCall.Enqueue();

				this.OnPropertyChanged("StringFilter");
			}
		}

		private void ProcessFilter() {
			foreach (PropertyInformation property in this.properties)
				property.Filter = this.propertyFilter;
		}

		public bool ShowDefaults {
			get { return this.propertyFilter.ShowDefaults; }
			set {
				this.propertyFilter.ShowDefaults = value;

				foreach (PropertyInformation property in this.properties)
					property.Filter = this.propertyFilter;

				this.OnPropertyChanged("ShowDefaults");
			}
		}
				

		private void HandleUnloaded(object sender, EventArgs e) {
			foreach (PropertyInformation property in this.properties)
				property.Teardown();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PropertyGrid2 propertyGrid = (PropertyGrid2)d;
			propertyGrid.SetTarget(e.NewValue);
		}

		private void HandleNameClick(object sender, MouseButtonEventArgs e) {
			if (e.ClickCount == 2) {
				PropertyInformation property = (PropertyInformation)((FrameworkElement)sender).DataContext;

				object newTarget = null;

				if (Keyboard.Modifiers == ModifierKeys.Shift)
					newTarget = property.Binding;
				else if (Keyboard.Modifiers == ModifierKeys.Control)
					newTarget = property.BindingExpression;
				else if (Keyboard.Modifiers == ModifierKeys.None)
					newTarget = property.Value;

				if (newTarget != null) {
					this.PushTarget(newTarget);
					//PropertyWindow window = new PropertyWindow(newTarget);
					//window.Show();
				}
			}
		}

		//private void Sort(string sortBy, ListSortDirection direction) {
		//    ICollectionView dataView = CollectionViewSource.GetDefaultView(this.properties);

		//    dataView.SortDescriptions.Clear();
		//    SortDescription sd = new SortDescription(sortBy, direction);
		//    dataView.SortDescriptions.Add(sd);
		//    dataView.Refresh();
		//}

		private void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction) {
			List<PropertyInformation> sorter = new List<PropertyInformation>(this.properties);
			sorter.Sort(comparator);

			if (direction == ListSortDirection.Descending)
				sorter.Reverse();

			this.properties.Clear();
			foreach (PropertyInformation property in sorter)
				this.properties.Add(property);
		}

		private static int CompareNames(PropertyInformation one, PropertyInformation two) {
			return string.Compare(one.DisplayName, two.DisplayName);
		}

		private static int CompareValues(PropertyInformation one, PropertyInformation two) {
			return string.Compare(one.StringValue, two.StringValue);
		}

		private static int CompareValueSources(PropertyInformation one, PropertyInformation two) {
			return string.Compare(one.ValueSource.BaseValueSource.ToString(), two.ValueSource.BaseValueSource.ToString());
		}
	}
}
