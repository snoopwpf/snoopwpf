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

		private Inspector inspector;

		public PropertyInspector() {

			this.InitializeComponent();

			this.inspector = this.PropertyGrid;
			this.inspector.Filter = this.propertyFilter;

			this.CommandBindings.Add(new CommandBinding(PropertyInspector.PopTargetCommand, this.HandlePopTarget, this.CanPopTarget));
			this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveCommand, this.HandleDelve, this.CanDelve));
			this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveBindingCommand, this.HandleDelveBinding, this.CanDelveBinding));
			this.CommandBindings.Add(new CommandBinding(PropertyInspector.DelveBindingExpressionCommand, this.HandleDelveBindingExpression, this.CanDelveBindingExpression));
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
			this.inspectStack.Add(target);
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
			if (this.inspectStack.Count > 1) {
				this.Target = this.inspectStack[this.inspectStack.Count - 2];
				this.inspectStack.RemoveAt(this.inspectStack.Count - 1);
				this.inspectStack.RemoveAt(this.inspectStack.Count - 1);
			}
		}

		private void CanPopTarget(object sender, CanExecuteRoutedEventArgs e) {
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

		public string StringFilter {
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

		private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
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
	}
}
