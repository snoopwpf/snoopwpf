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
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Snoop.Infrastructure;
using System.Linq;


namespace Snoop
{
	public class PropertyInformation : DependencyObject, IComparable, INotifyPropertyChanged
	{
		/// <summary>
		/// Normal constructor used when constructing PropertyInformation objects for properties.
		/// </summary>
		/// <param name="target">target object being shown in the property grid</param>
		/// <param name="property">the property around which we are contructing this PropertyInformation object</param>
		/// <param name="propertyName">the property name for the property that we use in the binding in the case of a non-dependency property</param>
		/// <param name="propertyDisplayName">the display name for the property that goes in the name column</param>
		public PropertyInformation(object target, PropertyDescriptor property, string propertyName, string propertyDisplayName)
		{
			this.target = target;
			this.property = property;
			this.displayName = propertyDisplayName;

			if (property != null)
			{
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
					binding = new Binding(propertyName);
				}

				binding.Source = target;
				binding.Mode = property.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;

				try
				{
					BindingOperations.SetBinding(this, PropertyInformation.ValueProperty, binding);
				}
				catch (Exception)
				{
					// cplotts note:
					// warning: i saw a problem get swallowed by this empty catch (Exception) block.
					// in other words, this empty catch block could be hiding some potential future errors.
				}
			}

			this.Update();

			this.isRunning = true;
		}

		/// <summary>
		/// Constructor used when constructing PropertyInformation objects for an item in a collection.
		/// In this case, we set the PropertyDescriptor for this object (in the property Property) to be null.
		/// This kind of makes since because an item in a collection really isn't a property on a class.
		/// That is, in this case, we're really hijacking the PropertyInformation class
		/// in order to expose the items in the Snoop property grid.
		/// </summary>
		/// <param name="target">the item in the collection</param>
		/// <param name="component">the collection</param>
		/// <param name="displayName">the display name that goes in the name column, i.e. this[x]</param>
		public PropertyInformation(object target, object component, string displayName, bool isCopyable = false)
			: this(target, null, displayName, displayName)
		{
			this.component = component;
            this.isCopyable = isCopyable;
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
				if (this.property == null)
				{
					// if this is a PropertyInformation object constructed for an item in a collection
					// then just return, since setting the value via a string doesn't make sense.
					return;
				}

				Type targetType = this.property.PropertyType;
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

		public string DescriptiveValue
		{
			get
			{
				object value = this.Value;
				if (value == null)
				{
					return string.Empty;
				}

				string stringValue = value.ToString();

				if (stringValue.Equals(value.GetType().ToString()))
				{
					// Add brackets around types to distinguish them from values.
					// Replace long type names with short type names for some specific types, for easier readability.
					// FUTURE: This could be extended to other types.
					if (this.property != null &&
						(this.property.PropertyType == typeof(Brush) || this.property.PropertyType == typeof(Style)))
					{
						stringValue = string.Format("[{0}]", value.GetType().Name);
					}
					else
					{
						stringValue = string.Format("[{0}]", stringValue);
					}
				}

				// Display #00FFFFFF as Transparent for easier readability
				if (this.property != null &&
					this.property.PropertyType == typeof(Brush) &&
					stringValue.Equals("#00FFFFFF"))
				{
					stringValue = "Transparent";
				}

				DependencyObject dependencyObject = this.Target as DependencyObject;
				if (dependencyObject != null && this.DependencyProperty != null)
				{
					// Cache the resource key for this item if not cached already. This could be done for more types, but would need to optimize perf.
					string resourceKey = null;
					if (this.property != null &&
						(this.property.PropertyType == typeof(Style) || this.property.PropertyType == typeof(Brush)))
					{
						object resourceItem = dependencyObject.GetValue(this.DependencyProperty);
						resourceKey = ResourceKeyCache.GetKey(resourceItem);
						if (string.IsNullOrEmpty(resourceKey))
						{
							resourceKey = ResourceDictionaryKeyHelpers.GetKeyOfResourceItem(dependencyObject, this.DependencyProperty);
							ResourceKeyCache.Cache(resourceItem, resourceKey);
						}
						Debug.Assert(resourceKey != null);
					}

					// Display both the value and the resource key, if there's a key for this property.
					if (!string.IsNullOrEmpty(resourceKey))
					{
						return string.Format("{0} {1}", resourceKey, stringValue);
					}

					// if the value comes from a Binding, show the path in [] brackets
					if ( IsExpression && Binding is Binding )
					{
						stringValue = string.Format( "{0} {1}", stringValue, BuildBindingDescriptiveString( (Binding)Binding, true ) );
					}

					// if the value comes from a MultiBinding, show the binding paths separated by , in [] brackets
					else if ( IsExpression && Binding is MultiBinding )
					{
						stringValue = stringValue + BuildMultiBindingDescriptiveString( ((MultiBinding)this.Binding).Bindings.OfType<Binding>().ToArray() );
					}

					// if the value comes from a PriorityBinding, show the binding paths separated by , in [] brackets
					else if ( IsExpression && Binding is PriorityBinding )
					{
						stringValue = stringValue + BuildMultiBindingDescriptiveString( ((PriorityBinding)this.Binding).Bindings.OfType<Binding>().ToArray() );
					}
				}

				return stringValue;
			}
		}

		/// <summary>
		/// Build up a string of Paths for a MultiBinding separated by ;
		/// </summary>
		private string BuildMultiBindingDescriptiveString( IEnumerable<Binding> bindings )
		{
			string ret = " {Paths=";
			foreach ( Binding binding in bindings )
			{
				ret += BuildBindingDescriptiveString( binding, false );
				ret += ";";
			}
			ret = ret.Substring( 0, ret.Length - 1 );	// remove trailing ,
			ret += "}";
			
			return ret;
		}

		/// <summary>
		/// Build up a string describing the Binding.  Path and ElementName (if present)
		/// </summary>
		private string BuildBindingDescriptiveString( Binding binding, bool isSinglePath )
		{
			var sb = new StringBuilder();
			var bindingPath = binding.Path.Path;
			var elementName = binding.ElementName;

			if ( isSinglePath )
			{
				sb.Append( "{Path=" );
			}

			sb.Append( bindingPath );
			if ( !String.IsNullOrEmpty( elementName )  )
			{
				sb.AppendFormat( ", ElementName={0}", elementName );
			}

			if ( isSinglePath )
			{
				sb.Append( "}" );
			}

			return sb.ToString();
		}

		public Type ComponentType
		{
			get
			{
				if (this.property == null)
				{
					// if this is a PropertyInformation object constructed for an item in a collection
					// then this.property will be null, but this.component will contain the collection.
					// use this object to return the type of the collection for the ComponentType.
					return this.component.GetType();
				}
				else
				{
					return this.property.ComponentType;
				}
			}
		}
		private object component;
        private bool isCopyable;

		public Type PropertyType
		{
			get
			{
				if (this.property == null)
				{
					// if this is a PropertyInformation object constructed for an item in a collection
					// just return typeof(object) here, since an item in a collection ... really isn't a property.
					return typeof(object);
				}
				else
				{
					return this.property.PropertyType;
				}
			}
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

		public bool IsValueChangedByUser { get; set; }


		public bool CanEdit
		{
			get
			{
				if (this.property == null)
				{
					// if this is a PropertyInformation object constructed for an item in a collection
					//return false;
                    return this.isCopyable;
				}
				else
				{
					return !this.property.IsReadOnly;
				}
			}
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
				if (this.property != null)
				{
					// in order to be a DependencyProperty, the object must first be a regular property,
					// and not an item in a collection.

					DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(this.property);
					if (dpd != null)
						return dpd.DependencyProperty;
				}

				return null;
			}
		}

		private void Update()
		{
			if (ignoreUpdate)
				return;

			this.isLocallySet = false;
			this.isInvalidBinding = false;
			this.isDatabound = false;

			DependencyProperty dp = this.DependencyProperty;
			DependencyObject d = target as DependencyObject;

			if (SnoopModes.MultipleDispatcherMode && d != null && d.Dispatcher != this.Dispatcher)
				return;

			if (dp != null && d != null)
			{
				if (d.ReadLocalValue(dp) != DependencyProperty.UnsetValue)
					this.isLocallySet = true;

				BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(d, dp);
				if (expression != null)
				{
					this.isDatabound = true;

                    if (expression.HasError || expression.Status != BindingStatus.Active && !(expression is PriorityBindingExpression))
					{
						this.isInvalidBinding = true;

						StringBuilder builder = new StringBuilder();
						StringWriter writer = new StringWriter(builder);
						TextWriterTraceListener tracer = new TextWriterTraceListener(writer);
						PresentationTraceSources.DataBindingSource.Listeners.Add(tracer);

						// reset binding to get the error message.
						ignoreUpdate = true;
						d.ClearValue(dp);
						BindingOperations.SetBinding(d, dp, expression.ParentBindingBase);
						ignoreUpdate = false;

						// cplotts note: maciek ... are you saying that this is another, more concise way to dispatch the following code?
						//Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
						//    {
						//        bindingError = builder.ToString();
						//        this.OnPropertyChanged("BindingError");
						//        PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
						//        writer.Close();
						//    });

						// this needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
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
			this.OnPropertyChanged("DescriptiveValue"); 
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
					PropertyInformation prop = new PropertyInformation(obj, property, property.Name, property.DisplayName);
					props.Add(prop);
				}
			}

            //delve path. also, issue 4919
            var extendedProps = GetExtendedProperties(obj);
            props.AddRange(extendedProps);


			// if the object is a collection, add the items in the collection as properties
			ICollection collection = obj as ICollection;
			int index = 0;
			if (collection != null)
			{
				foreach (object item in collection)
				{
					PropertyInformation info = new PropertyInformation(item, collection, "this[" + index + "]");
					index++;
					info.Value = item;
					props.Add(info);
				}
			}


			// sort the properties
			props.Sort();


			return props;
		}

        /// <summary>
        /// 4919 + Delve
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
		private static IList<PropertyInformation> GetExtendedProperties(object obj)
		{
			List<PropertyInformation> props = new List<PropertyInformation>();

			if (obj != null && ResourceKeyCache.Contains(obj))
			{
				string key = ResourceKeyCache.GetKey(obj);
				PropertyInformation prop = new PropertyInformation(key, new object(), "x:Key", true);
				prop.Value = key;
				props.Add(prop);
			}

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

                    var nextBaseTypeWithDefaultConstructor = GetNextTypeWithDefaultConstructor(t);
                    obj = Activator.CreateInstance(nextBaseTypeWithDefaultConstructor);
				}
			}

			return propertiesToReturn;
		}

        public static bool HasDefaultConstructor(Type type)
        {
            var constructors = type.GetConstructors();

            foreach (var constructor in constructors)
            {
                if (constructor.GetParameters().Length == 0)
                    return true;
            }
            return false;

        }

        public static Type GetNextTypeWithDefaultConstructor(Type type)
        {
            var t = type.BaseType;

            while (!HasDefaultConstructor(t))
                t = t.BaseType;

            return t;
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
		private bool ignoreUpdate = false;

        public bool IsCollection()
        {
            string pattern = "^this\\[\\d+\\]$";
            return Regex.IsMatch(this.DisplayName, pattern);
        }

        public int CollectionIndex()
        {
            if (this.IsCollection())
            {
                return int.Parse(this.DisplayName.Substring(5, this.DisplayName.Length - 6));
            }
            return -1;
        }

		#region IComparable Members
		public int CompareTo(object obj)
		{
            int thisIndex = this.CollectionIndex();
            int objIndex = ((PropertyInformation)obj).CollectionIndex();
            if (thisIndex >= 0 && objIndex >= 0)
            {
                return thisIndex.CompareTo(objIndex);
            }
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
