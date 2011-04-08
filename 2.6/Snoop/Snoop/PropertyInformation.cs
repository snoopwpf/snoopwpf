// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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

namespace Snoop
{
	public class PropertyInformation : DependencyObject, IComparable, INotifyPropertyChanged
	{
		public PropertyInformation(object target, PropertyDescriptor property, string displayName)
		{
			this.target = target;
			this.property = property;
			this.displayName = displayName;

			// create a data binding between the actual property value on the target object
			// and the Value dependency property on this PropertyInformation object
			Binding binding;
			DependencyProperty dp = this.DependencyProperty;
			if (dp != null)
			{
				binding = new Binding();
				binding.Path = new PropertyPath("(0)", new object[] { dp });
			}
			else
			{
				binding = new Binding(property.DisplayName);
			}
			binding.Source = target;
			binding.Mode = property.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;

			try
			{
				BindingOperations.SetBinding(this, PropertyInformation.ValueProperty, binding);
			}
			catch (Exception)
			{
			}

			this.Update();

			this.isRunning = true;
		}

		public void Teardown()
		{
			this.isRunning = false;
			BindingOperations.ClearAllBindings(this);
		}

		public object Target
		{
			get { return this.target; }
		}
		private object target;

		public object Value
		{
			get { return this.GetValue(PropertyInformation.ValueProperty); }
			set { this.SetValue(PropertyInformation.ValueProperty, value); }
		}
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register
			(
				"Value",
				typeof(object),
				typeof(PropertyInformation),
				new PropertyMetadata(new PropertyChangedCallback(PropertyInformation.HandleValueChanged))
			);
		private static void HandleValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((PropertyInformation)d).OnValueChanged();
		}
		protected virtual void OnValueChanged()
		{
			this.Update();

			if (this.isRunning)
			{
				if (this.breakOnChange)
				{
					if (!Debugger.IsAttached)
						Debugger.Launch();
					Debugger.Break();
				}

				this.HasChangedRecently = true;
				if (this.changeTimer == null)
				{
					this.changeTimer = new DispatcherTimer();
					this.changeTimer.Interval = TimeSpan.FromSeconds(1.5);
					this.changeTimer.Tick += this.HandleChangeExpiry;
					this.changeTimer.Start();
				}
				else
				{
					this.changeTimer.Stop();
					this.changeTimer.Start();
				}
			}
		}
		private void HandleChangeExpiry(object sender, EventArgs e)
		{
			this.changeTimer.Stop();
			this.changeTimer = null;

			this.HasChangedRecently = false;
		}
		private DispatcherTimer changeTimer;

		public string StringValue
		{
			get
			{
				object value = this.Value;
				if (value != null)
					return value.ToString();
				return string.Empty;
			}
			set
			{
				Type targetType = property.PropertyType;
				if (targetType.IsAssignableFrom(typeof(string)))
				{
					this.property.SetValue(this.target, value);
				}
				else
				{
					TypeConverter converter = TypeDescriptor.GetConverter(targetType);
					if (converter != null)
					{
						try
						{
							this.property.SetValue(this.target, converter.ConvertFrom(value));
						}
						catch (Exception)
						{
						}
					}
				}
			}
		}

		public Type ComponentType
		{
			get
			{
				return this.property.ComponentType;
			}
		}

		public Type PropertyType
		{
			get { return this.property.PropertyType; }
		}

		public Type ValueType
		{
			get
			{
				if (this.Value != null)
				{
					return this.Value.GetType();
				}
				else
				{
					return typeof(object);
				}
			}
		}

		public string BindingError
		{
			get { return this.bindingError; }
		}
		private string bindingError = string.Empty;

		public PropertyDescriptor Property
		{
			get { return this.property; }
		}
		private PropertyDescriptor property;

		public string DisplayName
		{
			get { return this.displayName; }
		}
		private string displayName;

		public bool IsInvalidBinding
		{
			get { return this.isInvalidBinding; }
		}
		private bool isInvalidBinding = false;

		public bool IsLocallySet
		{
			get { return this.isLocallySet; }
		}
		private bool isLocallySet = false;

		public bool CanEdit
		{
			get { return !this.property.IsReadOnly; }
		}

		public bool IsDatabound
		{
			get { return this.isDatabound; }
		}
		private bool isDatabound = false;

		public bool IsExpression
		{
			get { return this.valueSource.IsExpression; }
		}

		public bool IsAnimated
		{
			get { return this.valueSource.IsAnimated; }
		}

		public int Index
		{
			get { return this.index; }
			set {
				if (this.index != value)
				{
					this.index = value;
					this.OnPropertyChanged("Index");
					this.OnPropertyChanged("IsOdd");
				}
			}
		}
		private int index = 0;

		public bool IsOdd
		{
			get { return this.index % 2 == 1; }
		}

		public BindingBase Binding
		{
			get
			{
				DependencyProperty dp = this.DependencyProperty;
				DependencyObject d = this.target as DependencyObject;
				if (dp != null && d != null)
					return BindingOperations.GetBindingBase(d, dp);
				return null;
			}
		}

		public BindingExpressionBase BindingExpression
		{
			get
			{
				DependencyProperty dp = this.DependencyProperty;
				DependencyObject d = this.target as DependencyObject;
				if (dp != null && d != null)
					return BindingOperations.GetBindingExpressionBase(d, dp);
				return null;
			}
		}

		public PropertyFilter Filter
		{
			get { return this.filter; }
			set
			{
				this.filter = value;

				this.OnPropertyChanged("IsVisible");
			}
		}
		private PropertyFilter filter;

		public bool BreakOnChange
		{
			get { return this.breakOnChange; }
			set
			{
				this.breakOnChange = value;
				this.OnPropertyChanged("BreakOnChange");
			}
		}
		private bool breakOnChange = false;

		public bool HasChangedRecently
		{
			get { return this.changedRecently; }
			set
			{
				this.changedRecently = value;
				this.OnPropertyChanged("HasChangedRecently");
			}
		}
		private bool changedRecently = false;

		public ValueSource ValueSource
		{
			get { return this.valueSource; }
		}
		private ValueSource valueSource;

		public bool IsVisible
		{
			get { return this.filter.Show(this); }
		}

		public void Clear()
		{
			DependencyProperty dp = this.DependencyProperty;
			DependencyObject d = this.target as DependencyObject;
			if (dp != null && d != null)
				((DependencyObject)this.target).ClearValue(dp);
		}

		/// <summary>
		/// Returns the DependencyProperty identifier for the property that this PropertyInformation wraps.
		/// If the wrapped property is not a DependencyProperty, null is returned.
		/// </summary>
		private DependencyProperty DependencyProperty
		{
			get
			{
				DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(this.property);
				if (dpd != null)
					return dpd.DependencyProperty;
				return null;
			}
		}

		private void Update()
		{
			this.isLocallySet = false;
			this.isInvalidBinding = false;
			this.isDatabound = false;

			DependencyProperty dp = this.DependencyProperty;
			DependencyObject d = target as DependencyObject;
			if (dp != null && d != null)
			{

				if (d.ReadLocalValue(dp) != DependencyProperty.UnsetValue)
					this.isLocallySet = true;

				BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(d, dp);
				if (expression != null)
				{
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
						Dispatcher.BeginInvoke
						(
							DispatcherPriority.ApplicationIdle,
							new DispatcherOperationCallback
							(
								delegate(object source)
								{
									bindingError = builder.ToString();
									this.OnPropertyChanged("BindingError");
									PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
									writer.Close();
									return null;
								}
							),
							null
						);
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

		public static List<PropertyInformation> GetProperties(object obj)
		{
			return PropertyInformation.GetProperties(obj, new PertinentPropertyFilter(obj).Filter);
		}
		public static List<PropertyInformation> GetProperties(object obj, Predicate<PropertyDescriptor> filter)
		{
			List<PropertyInformation> props = new List<PropertyInformation>();


			// get the properties
			List<PropertyDescriptor> propertyDescriptors = GetAllProperties(obj, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) });


			// filter the properties
			foreach (PropertyDescriptor property in propertyDescriptors)
			{
				if (filter(property))
				{
					PropertyInformation prop = new PropertyInformation(obj, property, property.DisplayName);
					props.Add(prop);
				}
			}


			// if the object is a collection, add the items in the collection as properties
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


			// sort the properties
			props.Sort();


			return props;
		}

		private static List<PropertyDescriptor> GetAllProperties(object obj, Attribute[] attributes)
		{
			List<PropertyDescriptor> propertiesToReturn = new List<PropertyDescriptor>();

			// keep looping until you don't have an AmbiguousMatchException exception
			// and you normally won't have an exception, so the loop will typically execute only once.
			bool noException = false;
			while (!noException && obj != null)
			{
				try
				{
					// try to get the properties using the GetProperties method that takes an instance
					var properties = TypeDescriptor.GetProperties(obj, attributes);
					noException = true;

					MergeProperties(properties, propertiesToReturn);
				}
				catch (System.Reflection.AmbiguousMatchException)
				{
					// if we get an AmbiguousMatchException, the user has probably declared a property that hides a property in an ancestor
					// see issue 6258 (http://snoopwpf.codeplex.com/workitem/6258)
					//
					// public class MyButton : Button
					// {
					//     public new double? Width
					//     {
					//         get { return base.Width; }
					//         set { base.Width = value.Value; }
					//     }
					// }

					Type t = obj.GetType();
					var properties = TypeDescriptor.GetProperties(t, attributes);

					MergeProperties(properties, propertiesToReturn);

					obj = Activator.CreateInstance(t.BaseType);
				}
			}

			return propertiesToReturn;
		}
		private static void MergeProperties(System.Collections.IEnumerable newProperties, ICollection<PropertyDescriptor> allProperties)
		{
			foreach (var newProperty in newProperties)
			{
				PropertyDescriptor newPropertyDescriptor = newProperty as PropertyDescriptor;
				if (newPropertyDescriptor == null)
					continue;

				if (!allProperties.Contains(newPropertyDescriptor))
					allProperties.Add(newPropertyDescriptor);
			}
		}

		private bool isRunning = false;

		#region IComparable Members
		public int CompareTo(object obj)
		{
			return this.DisplayName.CompareTo(((PropertyInformation)obj).DisplayName);
		}
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
}
