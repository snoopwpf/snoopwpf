namespace Snoop {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Threading;

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

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(PropertyInformation), new PropertyMetadata(new PropertyChangedCallback(PropertyInformation.HandleValueChanged)));

		public PropertyInformation(object target, PropertyDescriptor property) {
			this.target = target;
			this.property = property;

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

		public PropertyDescriptor Property {
			get { return this.property; }
		}

		public string DisplayName {
			get { return this.property.DisplayName; }
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
						this.isInvalidBinding = true;
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
			return PropertyInformation.GetProperties(obj, delegate(PropertyDescriptor property) {
				return true;
			});
		}

		public static List<PropertyInformation> GetProperties(object obj, Predicate<PropertyDescriptor> filter) {
			List<PropertyInformation> props = new List<PropertyInformation>();


			PropertyDescriptorCollection propertyDescriptors = TypeDescriptor.GetProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });

			foreach (PropertyDescriptor property in propertyDescriptors) {
				if (filter(property)) {
					PropertyInformation prop = new PropertyInformation(obj, property);
					props.Add(prop);
				}
			}

			props.Sort();

			return props;
		}
	}

	//public class NiceValueSourceConverter : IValueConverter {
	//    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
	//        string description = string.Empty;
	//        ValueSource valueSource = (ValueSource)value;
	//        if (valueSource.IsAnimated)
	//            description += "Animation ";
	//        if (valueSource.IsCoerced)
	//            description += "Coerced ";
	//        if (valueSource.IsExpression)
	//            description += "Expression ";
	//    }

	//    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
	//        Debug.Assert("The method or operation is not implemented.");
	//        return null;
	//    }
		
	//}
}
