// Copyright © 2006 Microsoft Corporation.  All Rights Reserved

namespace Snoop {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Threading;
	using System.Collections;
	using System.Text;
	using System.IO;

	public class PropertyInformation: DependencyObject, IComparable, INotifyPropertyChanged
	{
		private PropertyDescriptor property;
		private object target;
		private bool isInvalidBinding = false;
		private bool isLocallySet = false;
		private PropertyFilter filter;
		private bool breakOnChange = false;
		private bool changedRecently = false;
		private bool isRunning = false;
		private bool isDatabound = false;
		private DispatcherTimer changeTimer;
		private ValueSource valueSource;
		private string bindingError = string.Empty;
		private string displayName;
		private int index = 0;

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(PropertyInformation), new PropertyMetadata(new PropertyChangedCallback(PropertyInformation.HandleValueChanged)));

		public PropertyInformation(object target, PropertyDescriptor property, string displayName) {
			this.target = target;
			this.property = property;
			this.displayName = displayName;

			Binding binding;
			DependencyProperty dp = this.DependencyProperty;
			if (dp != null) {
				binding = new Binding();
				binding.Path = new PropertyPath("(0)", new object[] { dp });
			}
			else
				binding = new Binding(property.DisplayName);

			binding.Source = target;
			try {
				BindingOperations.SetBinding(this, PropertyInformation.ValueProperty, binding);
			} catch (Exception) { }

			this.Update();

			this.isRunning = true;
		}

		~PropertyInformation() {
		}

		public void Teardown() {
			this.isRunning = false;
			BindingOperations.ClearAllBindings(this);
		}

		public object Target {
			get { return this.target; }
		}

		public object Value {
			get { return this.GetValue(PropertyInformation.ValueProperty); }
			set { this.SetValue(PropertyInformation.ValueProperty, value); }
		}

		public string StringValue {
			get {
				object value = this.Value;
				if (value != null)
					return value.ToString();
				return string.Empty;
			}
			set {
				Type targetType = property.PropertyType;
				if (targetType.IsAssignableFrom(typeof(string)))
					this.property.SetValue(this.target, value);
				else {
					TypeConverter converter = TypeDescriptor.GetConverter(targetType);
					if (converter != null) {
						try {
							this.property.SetValue(this.target, converter.ConvertFrom(value));
						} catch (Exception) { }
					}
				}
			}
		}

		public Type PropertyType {
			get { return this.property.PropertyType; }
		}

		public string BindingError {
			get { return this.bindingError; }
		}

		public PropertyDescriptor Property {
			get { return this.property; }
		}

		public string DisplayName {
			get { return this.displayName; }
		}

		public bool IsInvalidBinding {
			get { return this.isInvalidBinding; }
		}

		public bool IsLocallySet {
			get { return this.isLocallySet; }
		}

		public int CompareTo(object obj) {
			return this.DisplayName.CompareTo(((PropertyInformation)obj).DisplayName);
		}

		public bool CanEdit {
			get { return !this.property.IsReadOnly; }
		}

		public bool IsDatabound {
			get { return this.isDatabound; }
		}

		public bool IsExpression {
			get { return this.valueSource.IsExpression; }
		}

		public bool IsAnimated {
			get { return this.valueSource.IsAnimated; }
		}

		public int Index {
			get { return this.index; }
			set {
				if (this.index != value) {
					this.index = value;
					this.OnPropertyChanged("Index");
					this.OnPropertyChanged("IsOdd");
				}
			}
		}

		public bool IsOdd {
			get { return this.index % 2 == 1; }
		}

		public BindingBase Binding {
			get {
				DependencyProperty dp = this.DependencyProperty;
				DependencyObject d = this.target as DependencyObject;
				if (dp != null && d != null)
					return BindingOperations.GetBindingBase(d, dp);
				return null;
			}
		}

		public BindingExpressionBase BindingExpression {
			get {
				DependencyProperty dp = this.DependencyProperty;
				DependencyObject d = this.target as DependencyObject;
				if (dp != null && d != null)
					return BindingOperations.GetBindingExpressionBase(d, dp);
				return null;
			}
		}

		public PropertyFilter Filter {
			get { return this.filter; }
			set {
				this.filter = value;

				this.OnPropertyChanged("IsVisible");
			}
		}

		public bool BreakOnChange {
			get { return this.breakOnChange; }
			set {
				this.breakOnChange = value;
				this.OnPropertyChanged("BreakOnChange");
			}
		}

		public bool HasChangedRecently {
			get { return this.changedRecently; }
			set {
				this.changedRecently = value;
				this.OnPropertyChanged("HasChangedRecently");
			}
		}

		public ValueSource ValueSource {
			get { return this.valueSource; }
		}

		public bool IsVisible {
			get { return this.filter.Show(this); }
		}

		public void Clear() {
			DependencyProperty dp = this.DependencyProperty;
			DependencyObject d = this.target as DependencyObject;
			if (dp != null && d != null)
				((DependencyObject)this.target).ClearValue(dp);
		}

		protected virtual void OnValueChanged() {
			this.Update();

			if (this.isRunning) {
				if (this.breakOnChange) {
					if (!Debugger.IsAttached)
						Debugger.Launch();
					Debugger.Break();
				}

				this.HasChangedRecently = true;
				if (this.changeTimer == null) {
					this.changeTimer = new DispatcherTimer();
					this.changeTimer.Interval = TimeSpan.FromSeconds(1.5);
					this.changeTimer.Tick += this.HandleChangeExpiry;
					this.changeTimer.Start();
				}
				else {
					this.changeTimer.Stop();
					this.changeTimer.Start();
				}
			}
		}

		private void HandleChangeExpiry(object sender, EventArgs e) {
			this.changeTimer.Stop();
			this.changeTimer = null;

			this.HasChangedRecently = false;
		}

		private static void HandleValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			((PropertyInformation)d).OnValueChanged();
		}

		private DependencyProperty DependencyProperty {
			get {
				DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(this.property);
				if (dpd != null)
					return dpd.DependencyProperty;
				return null;
			}
		}

		private void Update() {
			this.isLocallySet = false;
			this.isInvalidBinding = false;
			this.isDatabound = false;

			DependencyProperty dp = this.DependencyProperty;
			DependencyObject d = target as DependencyObject;
			if (dp != null && d != null) {

				if (d.ReadLocalValue(dp) != DependencyProperty.UnsetValue)
					this.isLocallySet = true;

				BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(d, dp);
				if (expression != null) {
					this.isDatabound = true;

					if (expression.HasError || expression.Status != BindingStatus.Active)
					{
						this.isInvalidBinding = true;

						StringBuilder builder = new StringBuilder();
						StringWriter writer = new StringWriter(builder);
						TextWriterTraceListener tracer = new TextWriterTraceListener(writer);
						PresentationTraceSources.DataBindingSource.Listeners.Add(tracer);
						d.ClearValue(dp);
						BindingOperations.SetBinding(d, dp, expression.ParentBindingBase);

						// This needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
						Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new DispatcherOperationCallback(delegate(object source)
						{
							bindingError = builder.ToString();
							this.OnPropertyChanged("BindingError");
							PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
							writer.Close();
							return null;
						}), null);
					}
					else
					{
						bindingError = string.Empty;
					}
				}

				this.valueSource = DependencyPropertyHelper.GetValueSource(d, dp);
			}

			this.OnPropertyChanged("IsLocallySet");
			this.OnPropertyChanged("IsInvalidBinding");
			this.OnPropertyChanged("StringValue");
			this.OnPropertyChanged("IsDatabound");
			this.OnPropertyChanged("IsExpression");
			this.OnPropertyChanged("IsAnimated");
			this.OnPropertyChanged("ValueSource");
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) {
			Debug.Assert(this.GetType().GetProperty(propertyName) != null);
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public static List<PropertyInformation> GetProperties(object obj) {
			return PropertyInformation.GetProperties(obj, new PertinentPropertyFilter(obj).Filter);
		}

		public static List<PropertyInformation> GetProperties(object obj, Predicate<PropertyDescriptor> filter) {
			List<PropertyInformation> props = new List<PropertyInformation>();

			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });

			foreach (PropertyDescriptor property in propertyDescriptors) {
				if (filter(property)) {
					PropertyInformation prop = new PropertyInformation(obj, property, property.DisplayName);
					props.Add(prop);
				}
			}

			ICollection collection = obj as ICollection;
			int index = 0;
			if (collection != null)
			{
				foreach (object entry in collection)
				{
					PropertyInformation info = new PropertyInformation(entry, propertyDescriptors[0], "this[" + index + "]");
					index++;
					info.Value = entry;
					props.Add(info);
				}
			}

			props.Sort();

			return props;
		}
	}
}
