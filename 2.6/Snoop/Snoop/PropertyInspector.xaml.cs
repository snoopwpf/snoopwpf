// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Linq;

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
	using System.Collections;
	using System.Reflection;

	public partial class PropertyInspector: INotifyPropertyChanged
	{
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(object), typeof(PropertyInspector), new PropertyMetadata(PropertyInspector.HandleTargetChanged));
		public static readonly DependencyProperty RootTargetProperty = DependencyProperty.Register("RootTarget", typeof(object), typeof(PropertyInspector), new PropertyMetadata(PropertyInspector.HandleRootTargetChanged));

		public static readonly RoutedCommand PopTargetCommand = new RoutedCommand("PopTarget", typeof(PropertyInspector));
		public static readonly RoutedCommand DelveCommand = new RoutedCommand();
		public static readonly RoutedCommand DelveBindingCommand = new RoutedCommand();
		public static readonly RoutedCommand DelveBindingExpressionCommand = new RoutedCommand();

		//private object target;
		private PropertyFilter propertyFilter = new PropertyFilter(string.Empty, true);
		private List<object> inspectStack = new List<object>();
		private PropertyFilterSet[] _filterSets;

		private Inspector inspector;

		public PropertyInspector()
		{
			propertyFilter.SelectedFilterSet = AllFilterSets[0];
				
			this.InitializeComponent();

			this.inspector = this.PropertyGrid;
			this.inspector.Filter = this.propertyFilter;

			this.CommandBindings.Add(new CommandBinding(PropertyInspector.PopTargetCommand, this.HandlePopTarget, this.CanPopTarget));
			this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveCommand, this.HandleDelve, this.CanDelve));
			this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveBindingCommand, this.HandleDelveBinding, this.CanDelveBinding));
			this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveBindingExpressionCommand, this.HandleDelveBindingExpression, this.CanDelveBindingExpression));

			// don't show properties at their default values ... by default
			this.ShowDefaults = false;

			// watch for mouse "back" button
			this.MouseDown += new MouseButtonEventHandler(MouseDownHandler);
			this.KeyDown += new KeyEventHandler(PropertyInspector_KeyDown);
		}

		public object RootTarget {
			get { return this.GetValue(PropertyInspector.RootTargetProperty); }
			set { this.SetValue(PropertyInspector.RootTargetProperty, value); }
		}

		public object Target {
			get { return this.GetValue(PropertyInspector.TargetProperty); }
			set { this.SetValue(PropertyInspector.TargetProperty, value); }
		}

		public Type Type {
			get {
				if (this.Target != null)
					return this.Target.GetType();
				return null;
			}
		}

		public void PushTarget(object target) {
			this.Target = target;
		}

		

		/*
		public void SetTarget(object target) {
			this.inspectStack.Clear();
			this.Target = target;
		}*/
		/*
		private void ChangeTarget(object newTarget) {

			if (this.target != newTarget) {
				this.target = newTarget;

				this.OnPropertyChanged("Type");
			}
		}*/

		private void HandlePopTarget(object sender, ExecutedRoutedEventArgs e)
		{
			PopTarget();
		}

		private void PopTarget()
		{
			if (this.inspectStack.Count > 1)
			{
				this.Target = this.inspectStack[this.inspectStack.Count - 2];
				this.inspectStack.RemoveAt(this.inspectStack.Count - 2);
				this.inspectStack.RemoveAt(this.inspectStack.Count - 2);
			}
		}
		private void CanPopTarget(object sender, CanExecuteRoutedEventArgs e)
		{
			if (this.inspectStack.Count > 1) {
				e.Handled = true;
				e.CanExecute = true;
			}
		}

		private void HandleDelve(object sender, ExecutedRoutedEventArgs e) {
			this.PushTarget(((PropertyInformation)e.Parameter).Value);
			//this.Target = ((PropertyInformation)e.Parameter).Value;
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

		public PropertyFilter PropertyFilter
		{
			get { return propertyFilter; }
		}

		public string StringFilter
		{
			get { return this.propertyFilter.FilterString; }
			set {
				this.propertyFilter.FilterString = value;

				this.inspector.Filter = this.propertyFilter;

				this.OnPropertyChanged("StringFilter");
			}
		}

		public bool ShowDefaults {
			get { return this.propertyFilter.ShowDefaults; }
			set {
				this.propertyFilter.ShowDefaults = value;

				this.inspector.Filter = this.propertyFilter;

				this.OnPropertyChanged("ShowDefaults");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			PropertyInspector inspector = (PropertyInspector)d;
			inspector.OnPropertyChanged("Type");

			if (e.NewValue != null)
				inspector.inspectStack.Add(e.NewValue);
		}

		private static void HandleRootTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PropertyInspector inspector = (PropertyInspector)d;

			inspector.inspectStack.Clear();
			inspector.Target = e.NewValue;
		}

		#region DH (Dan Added)
		/// <summary>
		/// Looking for "browse back" mouse button.
		/// Pop properties context when clicked.
		/// </summary>
		void MouseDownHandler(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.XButton1)
			{
				PopTarget();
			}
		}
		void PropertyInspector_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Left)
			{
				PopTarget();
			}
		}
		#endregion

		#region DH Default FilterSets
		private readonly PropertyFilterSet[] _defaultFilterSets = new PropertyFilterSet[]
		{
			//new PropertyFilterSet 
			//{
			//    DisplayName = "(Default)",
			//    IsDefault = true
			//},
			new PropertyFilterSet
			{
				DisplayName = "Layout",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"width", "height", "actualwidth", "actualheight", "margin", "padding", "left", "top"
				}
			},
			new PropertyFilterSet
			{
				DisplayName = "Grid/Dock",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"grid", "dock"
				}
			},
			new PropertyFilterSet
			{
				DisplayName = "Color",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"color", "background", "foreground", "borderbrush", "fill", "stroke"
				}
			},
			new PropertyFilterSet
			{
				DisplayName = "ItemsControl",
				IsDefault = false,
				IsEditCommand = false,
				Properties = new string[]
				{
					"items", "selected"
				}
			}
		};
		#endregion

		/// <summary>
		/// Hold the SelectedFilterSet in the PropertyFilter class, but track it here, so we know
		/// when to "refresh" the filtering with filterCall.Enqueue
		/// </summary>
		public PropertyFilterSet SelectedFilterSet
		{
			get { return propertyFilter.SelectedFilterSet; }
			set
			{
				propertyFilter.SelectedFilterSet = value;
				OnPropertyChanged( "SelectedFilterSet" );
				
				if ( value == null )
					return;

				if ( value.IsEditCommand )
				{
					var dlg = new EditUserFilters { UserFilters = CopyFilterSets( UserFilterSets ) };

					// set owning window to center over if we can find it up the tree
					var snoopWindow = VisualTreeHelper2.GetAncestor<Window>( this );
					if ( snoopWindow != null )
					{
						dlg.Owner = snoopWindow;
						dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					}

					bool? res = dlg.ShowDialog();
					if ( res.GetValueOrDefault() )
					{
						// take the adjusted values from the dialog, setter will SAVE them to user properties
						UserFilterSets = CleansFilterPropertyNames( dlg.ItemsSource );
						// trigger the UI to re-bind to the collection, so user sees changes they just made
						OnPropertyChanged( "AllFilterSets" );
					}

					// now that we're out of the dialog, set current selection back to "(default)"
					Dispatcher.BeginInvoke( DispatcherPriority.Background, (DispatcherOperationCallback)delegate
					{
						//DHDH - couldnt get it working by setting SelectedFilterSet directly
						// using the Index to get us back to the first item in the list
						FilterSetCombo.SelectedIndex = 0;
						//SelectedFilterSet = AllFilterSets[0];
						return null;
					}, null );
				}
				else
				{
					this.inspector.Filter = this.propertyFilter;
					OnPropertyChanged( "SelectedFilterSet" );
				}
			}
		}

		/// <summary>
		/// Get or Set the collection of User filter sets.  These are the filters that are configurable by 
		/// the user, and serialized to/from app Settings.
		/// </summary>
		public PropertyFilterSet[] UserFilterSets
		{
			get
			{
				if ( _filterSets == null )
				{
					var ret = new List<PropertyFilterSet>();

					try
					{
						var userFilters = Properties.Settings.Default.PropertyFilterSets;
						ret.AddRange( userFilters ?? _defaultFilterSets );
					}
					catch ( Exception ex )
					{
						string msg = String.Format( "Error reading user filters from settings. Using defaults.\r\n\r\n{0}", ex.Message );
						MessageBox.Show( msg, "Exception", MessageBoxButton.OK, MessageBoxImage.Error );
						ret.Clear();
						ret.AddRange( _defaultFilterSets );
					}

					_filterSets = ret.ToArray();
				}
				return _filterSets;
			}
			set
			{
				_filterSets = value;
				Properties.Settings.Default.PropertyFilterSets = _filterSets;
				Properties.Settings.Default.Save();
			}
		}

		/// <summary>
		/// Get the collection of "all" filter sets.  This is the UserFilterSets wrapped with 
		/// (Default) at the start and "Edit Filters..." at the end of the collection.
		/// This is the collection bound to in the UI 
		/// </summary>
		public PropertyFilterSet[] AllFilterSets
		{
			get
			{
				var ret = new List<PropertyFilterSet>( UserFilterSets );

				// now add the "(Default)" and "Edit Filters..." filters for the ComboBox
				ret.Insert( 0, new PropertyFilterSet
					            {
					               	DisplayName = "(Default)",
					               	IsDefault = true,
					               	IsEditCommand = false,
					            } );
				ret.Add( new PropertyFilterSet
					        {
					         	DisplayName = "Edit Filters...",
					         	IsDefault = false,
					         	IsEditCommand = true,
					        } );
				return ret.ToArray();
			}
		}

		/// <summary>
		/// Make a deep copy of the filter collection.
		/// This is used when heading into the Edit dialog, so the user is editing a copy of the
		/// filters, in case they cancel the dialog - we dont want to alter their live collection.
		/// </summary>
		public PropertyFilterSet[] CopyFilterSets( PropertyFilterSet[] source )
		{
			var ret = new List<PropertyFilterSet>();
			foreach ( PropertyFilterSet src in source )
			{
				ret.Add( new PropertyFilterSet
				         	{
				         		DisplayName = src.DisplayName,
				         		IsDefault = src.IsDefault,
				         		IsEditCommand = src.IsEditCommand,
				         		Properties = (string[])src.Properties.Clone()
				         	} );
			}

			return ret.ToArray();
		}

		/// <summary>
		/// Cleanse the property names in each filter in the collection.
		/// This includes removing spaces from each one, and making them all lower case
		/// </summary>
		private PropertyFilterSet[] CleansFilterPropertyNames( IEnumerable<PropertyFilterSet> collection )
		{
			foreach ( PropertyFilterSet filterItem in collection )
			{
				filterItem.Properties = filterItem.Properties.Select( s => s.ToLower().Trim() ).ToArray();
			}
			return collection.ToArray();
		}


	}
}
