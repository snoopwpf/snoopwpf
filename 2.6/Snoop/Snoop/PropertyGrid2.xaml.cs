// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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
using Snoop.Infrastructure;

namespace Snoop
{
	public partial class PropertyGrid2 : INotifyPropertyChanged
	{
		public static readonly RoutedCommand ShowBindingErrorsCommand = new RoutedCommand();
		public static readonly RoutedCommand ClearCommand = new RoutedCommand();
		public static readonly RoutedCommand SortCommand = new RoutedCommand();


		public PropertyGrid2()
		{
			this.processIncrementalCall = new DelayedCall(this.ProcessIncrementalPropertyAdd, DispatcherPriority.Background);
			this.filterCall = new DelayedCall(this.ProcessFilter, DispatcherPriority.Background);

			this.InitializeComponent();

			this.Loaded += this.HandleLoaded;
			this.Unloaded += this.HandleUnloaded;

			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.ShowBindingErrorsCommand, this.HandleShowBindingErrors, this.CanShowBindingErrors));
			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.ClearCommand, this.HandleClear, this.CanClear));
			this.CommandBindings.Add(new CommandBinding(PropertyGrid2.SortCommand, this.HandleSort));
		}


		public ObservableCollection<PropertyInformation> Properties
		{
			get { return this.properties; }
		}
		private ObservableCollection<PropertyInformation> properties = new ObservableCollection<PropertyInformation>();

		public object Target
		{
			get { return this.GetValue(PropertyGrid2.TargetProperty); }
			set { this.SetValue(PropertyGrid2.TargetProperty, value); }
		}
		public static readonly DependencyProperty TargetProperty =
			DependencyProperty.Register
			(
				"Target",
				typeof(object),
				typeof(PropertyGrid2),
				new PropertyMetadata(new PropertyChangedCallback(PropertyGrid2.HandleTargetChanged))
			);
		private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			PropertyGrid2 propertyGrid = (PropertyGrid2)d;
			propertyGrid.ChangeTarget(e.NewValue);
		}
		private void ChangeTarget(object newTarget)
		{
			if (this.target != newTarget)
			{
				this.target = newTarget;

				foreach (PropertyInformation property in this.properties)
					property.Teardown();

				this.RefreshPropertyGrid();

				this.OnPropertyChanged("Type");
			}
		}

		public PropertyInformation Selection
		{
			get { return this.selection; }
			set
			{
				this.selection = value;
				this.OnPropertyChanged("Selection");
			}
		}
		private PropertyInformation selection;

		public Type Type
		{
			get
			{
				if (this.target != null)
					return this.target.GetType();
				return null;
			}
		}


		protected override void OnFilterChanged()
		{
			base.OnFilterChanged();

			this.filterCall.Enqueue();
		}


		/// <summary>
		/// Delayed loading of the property inspector to avoid creating the entire list of property
		/// editors immediately after selection. Keeps that app running smooth.
		/// </summary>
		/// <param name="performInitialization"></param>
		/// <returns></returns>
		private void ProcessIncrementalPropertyAdd()
		{
			int numberToAdd = 10;

			if (this.propertiesToAdd == null)
			{
				this.propertiesToAdd = PropertyInformation.GetProperties(this.target).GetEnumerator();

				numberToAdd = 0;
			}
			int i = 0;
			for (; i < numberToAdd && this.propertiesToAdd.MoveNext(); ++i)
			{
				// iterate over the PropertyInfo objects,
				// setting the property grid's filter on each object,
				// and adding those properties to the observable collection of properties (this.properties)
				PropertyInformation property = this.propertiesToAdd.Current;
				property.Filter = this.Filter;
				this.properties.Add(property);

				// checking whether a property is visible ... actually runs the property filtering code
				if (property.IsVisible)
					property.Index = this.visiblePropertyCount++;

				// cplotts todo:
				// i wonder if i add just the properties that are visible
				// if that will get rid of the weird scrolling problem when properties have been filtered out.
				//
				// nope.
				// i quickly tried moving the this.properties.Add(property) method into the if (property.IsVisible) statement,
				// but that didn't work when filtering and unfiltering
				// (e.g. showing properties at their default value ... and NOT showing properties at their default value).
			}

			if (i == numberToAdd)
				this.processIncrementalCall.Enqueue();
			else
				this.propertiesToAdd = null;
		}

		private void HandleShowBindingErrors(object sender, ExecutedRoutedEventArgs eventArgs)
		{
			PropertyInformation propertyInformation = (PropertyInformation)eventArgs.Parameter;
			Window window = new Window();
			TextBox textbox = new TextBox();
			textbox.IsReadOnly = true;
			textbox.Text = propertyInformation.BindingError;
			textbox.TextWrapping = TextWrapping.Wrap;
			window.Content = textbox;
			window.Width = 400;
			window.Height = 300;
			window.Title = "Binding Errors for " + propertyInformation.DisplayName;
			SnoopPartsRegistry.AddSnoopVisualTreeRoot(window);
			window.Closing +=
				(s, e) =>
				{
					Window w = (Window)s;
					SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(w);
				};
			window.Show();
		}
		private void CanShowBindingErrors(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Parameter != null && !string.IsNullOrEmpty(((PropertyInformation)e.Parameter).BindingError))
				e.CanExecute = true;
			e.Handled = true;
		}

		private void CanClear(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Parameter != null && ((PropertyInformation)e.Parameter).IsLocallySet)
				e.CanExecute = true;
			e.Handled = true;
		}
		private void HandleClear(object sender, ExecutedRoutedEventArgs e)
		{
			((PropertyInformation)e.Parameter).Clear();
		}

		private void HandleSort(object sender, ExecutedRoutedEventArgs args)
		{
			GridViewColumnHeader headerClicked = (GridViewColumnHeader)args.OriginalSource;

			if (headerClicked != null)
			{
				ListSortDirection direction = ListSortDirection.Ascending;

				if (headerClicked == this.lastHeaderClicked && this.lastDirection == ListSortDirection.Ascending)
					direction = ListSortDirection.Descending;

				switch (((TextBlock)headerClicked.Column.Header).Text)
				{
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

		private void ProcessFilter()
		{
			int index = 0;
			foreach (PropertyInformation property in this.properties)
			{
				property.Filter = this.Filter;
				property.Index = index;
				if (property.IsVisible)
					++index;
			}
		}

		private void HandleLoaded(object sender, EventArgs e)
		{
			if (this.unloaded)
			{
				this.RefreshPropertyGrid();
				this.unloaded = false;
			}
		}
		private void HandleUnloaded(object sender, EventArgs e)
		{
			foreach (PropertyInformation property in this.properties)
				property.Teardown();

			unloaded = true;
		}

		private void HandleNameClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				PropertyInformation property = (PropertyInformation)((FrameworkElement)sender).DataContext;

				object newTarget = null;

				if (Keyboard.Modifiers == ModifierKeys.Shift)
					newTarget = property.Binding;
				else if (Keyboard.Modifiers == ModifierKeys.Control)
					newTarget = property.BindingExpression;
				else if (Keyboard.Modifiers == ModifierKeys.None)
					newTarget = property.Value;

				if (newTarget != null)
				{
					PropertyInspector.DelveCommand.Execute(property, this);
				}
			}
		}

		private void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction)
		{
			List<PropertyInformation> sorter = new List<PropertyInformation>(this.properties);
			sorter.Sort(comparator);

			if (direction == ListSortDirection.Descending)
				sorter.Reverse();

			this.properties.Clear();
			foreach (PropertyInformation property in sorter)
				this.properties.Add(property);
		}

		private void RefreshPropertyGrid()
		{
			this.properties.Clear();
			this.visiblePropertyCount = 0;

			this.propertiesToAdd = null;
			this.processIncrementalCall.Enqueue();
		}


		private object target;

		private IEnumerator<PropertyInformation> propertiesToAdd;

		private GridViewColumnHeader lastHeaderClicked = null;
		private ListSortDirection lastDirection = ListSortDirection.Ascending;
		private DelayedCall processIncrementalCall;
		private DelayedCall filterCall;
		private int visiblePropertyCount = 0;
		private bool unloaded = false;


		private static int CompareNames(PropertyInformation one, PropertyInformation two)
		{
			return string.Compare(one.DisplayName, two.DisplayName);
		}
		private static int CompareValues(PropertyInformation one, PropertyInformation two) {
			return string.Compare(one.StringValue, two.StringValue);
		}
		private static int CompareValueSources(PropertyInformation one, PropertyInformation two) {
			return string.Compare(one.ValueSource.BaseValueSource.ToString(), two.ValueSource.BaseValueSource.ToString());
		}


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
}
