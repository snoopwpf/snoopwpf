// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Threading;
    using JetBrains.Annotations;
    using Snoop.Converters;
    using Snoop.Infrastructure;

    public class PropertyInformation : DependencyObject, IComparable, INotifyPropertyChanged
    {
        /// <summary>
        /// Normal constructor used when constructing PropertyInformation objects for properties.
        /// </summary>
        /// <param name="target">target object being shown in the property grid</param>
        /// <param name="property">the property around which we are constructing this PropertyInformation object</param>
        /// <param name="propertyName">the property name for the property that we use in the binding in the case of a non-dependency property</param>
        /// <param name="propertyDisplayName">the display name for the property that goes in the name column</param>
        public PropertyInformation(object target, PropertyDescriptor property, string propertyName, string propertyDisplayName)
        {
            this.Target = target;
            this.property = property;
            this.displayName = propertyDisplayName;

            if (property != null)
            {
                // create a data binding between the actual property value on the target object
                // and the Value dependency property on this PropertyInformation object
                Binding binding;
                var dp = this.DependencyProperty;
                if (dp != null)
                {
                    binding = new Binding();
                    binding.Path = new PropertyPath("(0)", new object[] { dp });

                    if (dp == FrameworkElement.StyleProperty
                        || dp == FrameworkContentElement.StyleProperty)
                    {
                        binding.Converter = NullStyleConverter.DefaultInstance;
                        binding.ConverterParameter = target;
                    }
                }
                else
                {
                    binding = new Binding(propertyName);
                }

                binding.Source = target;
                binding.Mode = property.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;

                try
                {
                    BindingOperations.SetBinding(this, ValueProperty, binding);
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
        /// Normal constructor used when constructing PropertyInformation objects for properties.
        /// </summary>
        /// <param name="target">target object being shown in the property grid</param>
        /// <param name="property">the property around which we are constructing this PropertyInformation object</param>
        /// <param name="binding">the <see cref="BindingBase"/> from which the value should be retrieved</param>
        /// <param name="propertyDisplayName">the display name for the property that goes in the name column</param>
        public PropertyInformation(object target, PropertyDescriptor property, BindingBase binding, string propertyDisplayName)
        {
            this.Target = target;
            this.property = property;
            this.displayName = propertyDisplayName;

            try
            {
                BindingOperations.SetBinding(this, ValueProperty, binding);
            }
            catch (Exception)
            {
                // cplotts note:
                // warning: i saw a problem get swallowed by this empty catch (Exception) block.
                // in other words, this empty catch block could be hiding some potential future errors.
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

        public object Target { get; }

        public object Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(PropertyInformation),
                new PropertyMetadata(HandleValueChanged));

        private static void HandleValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PropertyInformation)d).OnValueChanged(e);
        }

        protected virtual void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            this.Update();

            if (this.isRunning)
            {
                if (this.breakOnChange)
                {
                    if (Debugger.IsAttached == false)
                    {
                        Debugger.Launch();
                    }

                    Debugger.Break();
                }

                this.HasChangedRecently = (e.OldValue?.Equals(e.NewValue) ?? e.OldValue == e.NewValue) == false;

                if (this.changeTimer == null)
                {
                    this.changeTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.5)
                    };
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
                var value = this.Value;
                if (value != null)
                {
                    return value.ToString();
                }

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

                var targetType = this.property.PropertyType;

                try
                {
                    this.property.SetValue(this.Target, StringValueConverter.ConvertFromString(targetType, value));
                }
                catch
                {
                }
            }
        }

        public string ResourceKey
        {
            get => this.resourceKey;
            private set
            {
                if (value == this.resourceKey)
                {
                    return;
                }

                this.resourceKey = value;
                this.OnPropertyChanged(nameof(this.ResourceKey));
            }
        }

        public string DescriptiveValue
        {
            get
            {
                var value = this.Value;
                if (value == null)
                {
                    return string.Empty;
                }

                var stringValue = value.ToString();

                if (stringValue.Equals(value.GetType().ToString()))
                {
                    // Add brackets around types to distinguish them from values.
                    // Replace long type names with short type names for some specific types, for easier readability.
                    // FUTURE: This could be extended to other types.
                    if (value is BindingBase)
                    {
#pragma warning disable INPC013
                        stringValue = string.Format("[{0}]", "Binding");
#pragma warning restore INPC013
                    }
                    else if (value is DynamicResourceExtension)
                    {
                        stringValue = string.Format("[{0}]", "DynamicResource");
                    }
                    else if (this.property != null &&
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

                if (this.Target is DependencyObject dependencyObject)
                {
                    // Cache the resource key for this item if not cached already. This could be done for more types, but would need to optimize perf.
                    this.ResourceKey = null;

                    if (this.property != null
                        && (this.property.PropertyType == typeof(Style) || this.property.PropertyType == typeof(Brush)))
                    {
                        var resourceItem = value;
                        this.ResourceKey = ResourceKeyCache.GetKey(resourceItem);

                        if (string.IsNullOrEmpty(this.ResourceKey))
                        {
                            this.ResourceKey = ResourceDictionaryKeyHelpers.GetKeyOfResourceItem(dependencyObject, resourceItem);
                            ResourceKeyCache.Cache(resourceItem, this.ResourceKey);
                        }

                        Debug.Assert(this.ResourceKey != null, "this.ResourceKey != null");
                    }

                    // Display both the value and the resource key, if there's a key for this property.
                    if (string.IsNullOrEmpty(this.ResourceKey) == false)
                    {
                        return string.Format("{0} {1}", this.ResourceKey, stringValue);
                    }

                    // if the value comes from a Binding, show the path in [] brackets
                    if (this.IsExpression
                        && this.Binding is Binding)
                    {
                        stringValue = string.Format("{0} {1}", stringValue, this.BuildBindingDescriptiveString((Binding)this.Binding, true));
                    }

                    // if the value comes from a MultiBinding, show the binding paths separated by , in [] brackets
                    else if (this.IsExpression
                        && this.Binding is MultiBinding)
                    {
                        stringValue += this.BuildMultiBindingDescriptiveString(((MultiBinding)this.Binding).Bindings.OfType<Binding>().ToArray());
                    }

                    // if the value comes from a PriorityBinding, show the binding paths separated by , in [] brackets
                    else if (this.IsExpression && this.Binding is PriorityBinding)
                    {
                        stringValue += this.BuildMultiBindingDescriptiveString(((PriorityBinding)this.Binding).Bindings.OfType<Binding>().ToArray());
                    }
                }

                return stringValue;
            }
        }

        /// <summary>
        /// Build up a string of Paths for a MultiBinding separated by ;
        /// </summary>
        private string BuildMultiBindingDescriptiveString(IEnumerable<Binding> bindings)
        {
            var ret = " {Paths=";
            foreach (var binding in bindings)
            {
                ret += this.BuildBindingDescriptiveString(binding, false);
                ret += ";";
            }

            ret = ret.Substring(0, ret.Length - 1); // remove trailing ,
            ret += "}";

            return ret;
        }

        /// <summary>
        /// Build up a string describing the Binding.  Path and ElementName (if present)
        /// </summary>
        private string BuildBindingDescriptiveString(Binding binding, bool isSinglePath)
        {
            var sb = new StringBuilder();
            var bindingPath = binding.Path.Path;
            var elementName = binding.ElementName;

            if (isSinglePath)
            {
                sb.Append("{Path=");
            }

            sb.Append(bindingPath);
            if (!string.IsNullOrEmpty(elementName))
            {
                sb.AppendFormat(", ElementName={0}", elementName);
            }

            if (isSinglePath)
            {
                sb.Append("}");
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

        private readonly object component;
        private readonly bool isCopyable;

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

        private readonly PropertyDescriptor property;

        public string DisplayName
        {
            get { return this.displayName; }
        }

        private readonly string displayName;

        public bool IsInvalidBinding
        {
            get { return this.isInvalidBinding; }
        }

        private bool isInvalidBinding;

        public bool IsLocallySet
        {
            get { return this.isLocallySet; }
        }

        private bool isLocallySet;

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

        private bool isDatabound;

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

            set
            {
                if (this.index != value)
                {
                    this.index = value;
                    this.OnPropertyChanged(nameof(this.Index));
                    this.OnPropertyChanged(nameof(this.IsOdd));
                }
            }
        }

        private int index;

        public bool IsOdd
        {
            get { return this.index % 2 == 1; }
        }

        public BindingBase Binding
        {
            get
            {
                var dp = this.DependencyProperty;
                var d = this.Target as DependencyObject;
                if (dp != null && d != null)
                {
                    return BindingOperations.GetBindingBase(d, dp);
                }

                return null;
            }
        }

        public BindingExpressionBase BindingExpression
        {
            get
            {
                var dp = this.DependencyProperty;
                var d = this.Target as DependencyObject;
                if (dp != null && d != null)
                {
                    return BindingOperations.GetBindingExpressionBase(d, dp);
                }

                return null;
            }
        }

        public PropertyFilter Filter
        {
            get { return this.filter; }

            set
            {
                this.filter = value;

                this.OnPropertyChanged(nameof(this.IsVisible));
            }
        }

        private PropertyFilter filter;

        public bool BreakOnChange
        {
            get { return this.breakOnChange; }

            set
            {
                this.breakOnChange = value;
                this.OnPropertyChanged(nameof(this.BreakOnChange));
            }
        }

        private bool breakOnChange;

        public bool HasChangedRecently
        {
            get { return this.hasChangedRecently; }

            set
            {
                this.hasChangedRecently = value;
                this.OnPropertyChanged(nameof(this.HasChangedRecently));
            }
        }

        private bool hasChangedRecently;

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
            var dp = this.DependencyProperty;
            var d = this.Target as DependencyObject;
            if (dp != null && d != null)
            {
                ((DependencyObject)this.Target).ClearValue(dp);
            }
        }

        /// <summary>
        /// Returns the DependencyProperty identifier for the property that this PropertyInformation wraps.
        /// If the wrapped property is not a DependencyProperty, null is returned.
        /// </summary>
        public DependencyProperty DependencyProperty
        {
            get
            {
                if (this.property != null)
                {
                    // in order to be a DependencyProperty, the object must first be a regular property,
                    // and not an item in a collection.

                    var dpd = DependencyPropertyDescriptor.FromProperty(this.property);
                    if (dpd != null)
                    {
                        return dpd.DependencyProperty;
                    }
                }

                return null;
            }
        }

        private void Update()
        {
            if (this.ignoreUpdate)
            {
                return;
            }

            this.isLocallySet = false;
            this.isInvalidBinding = false;
            this.isDatabound = false;

            var dp = this.DependencyProperty;
            var d = this.Target as DependencyObject;

            if (SnoopModes.MultipleDispatcherMode && d != null && d.Dispatcher != this.Dispatcher)
            {
                return;
            }

            if (dp != null && d != null)
            {
                if (d.ReadLocalValue(dp) != DependencyProperty.UnsetValue)
                {
                    this.isLocallySet = true;
                }

                var expression = BindingOperations.GetBindingExpressionBase(d, dp);
                if (expression != null)
                {
                    this.isDatabound = true;

                    if (expression.HasError 
                        || (expression.Status != BindingStatus.Active && !(expression is PriorityBindingExpression)))
                    {
                        this.isInvalidBinding = true;

                        var builder = new StringBuilder();
                        var writer = new StringWriter(builder);
                        var tracer = new TextWriterTraceListener(writer);
                        PresentationTraceSources.DataBindingSource.Listeners.Add(tracer);

                        // reset binding to get the error message.
                        this.ignoreUpdate = true;
                        d.ClearValue(dp);
                        BindingOperations.SetBinding(d, dp, expression.ParentBindingBase);
                        this.ignoreUpdate = false;

                        // cplotts note: maciek ... are you saying that this is another, more concise way to dispatch the following code?
                        //Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
                        //    {
                        //        bindingError = builder.ToString();
                        //        this.OnPropertyChanged("BindingError");
                        //        PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
                        //        writer.Close();
                        //    });

                        // this needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
                        this.Dispatcher.BeginInvoke(
                            DispatcherPriority.ApplicationIdle,
                            new DispatcherOperationCallback(
                                delegate
                                {
                                    this.bindingError = builder.ToString();
                                    this.OnPropertyChanged(nameof(this.BindingError));
                                    PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
                                    writer.Close();
                                    return null;
                                }),
                            null);
                    }
                    else
                    {
                        this.bindingError = string.Empty;
                    }
                }

                this.valueSource = DependencyPropertyHelper.GetValueSource(d, dp);
            }

            this.OnPropertyChanged(nameof(this.IsLocallySet));
            this.OnPropertyChanged(nameof(this.IsInvalidBinding));
            this.OnPropertyChanged(nameof(this.StringValue));
            this.OnPropertyChanged(nameof(this.DescriptiveValue));
            this.OnPropertyChanged(nameof(this.IsDatabound));
            this.OnPropertyChanged(nameof(this.IsExpression));
            this.OnPropertyChanged(nameof(this.IsAnimated));
            this.OnPropertyChanged(nameof(this.ValueSource));
        }

        public static List<PropertyInformation> GetProperties(object obj)
        {
            return GetProperties(obj, PertinentPropertyFilter.Filter);
        }

        public static List<PropertyInformation> GetProperties(object obj, Func<object, PropertyDescriptor, bool> filter)
        {
            var properties = new List<PropertyInformation>();

            if (obj is null)
            {
                return properties;
            }

            // get the properties
            var propertyDescriptors = GetAllProperties(obj, getAllPropertiesAttributeFilter);

            // filter the properties
            foreach (var property in propertyDescriptors)
            {
                if (filter(obj, property))
                {
                    var prop = new PropertyInformation(obj, property, property.Name, property.DisplayName);
                    properties.Add(prop);
                }
            }

            //delve path. also, issue 4919
            var extendedProps = GetExtendedProperties(obj);
            if (extendedProps != null)
            {
                properties.AddRange(extendedProps);
            }

            // if the object is a collection, add the items in the collection as properties
            if (obj is ICollection collection)
            {
                var index = 0;
                foreach (var item in collection)
                {
                    var info = new PropertyInformation(item, collection, "this[" + index + "]");
                    index++;
                    info.Value = item;
                    properties.Add(info);
                }
            }

            // sort the properties
            properties.Sort();

            return properties;
        }

        /// <summary>
        /// 4919 + Delve
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
		private static IList<PropertyInformation> GetExtendedProperties(object obj)
        {
            if (obj is null
                || ResourceKeyCache.Contains(obj) == false)
            {
                return null;
            }

            var key = ResourceKeyCache.GetKey(obj);
            var prop = new PropertyInformation(key, new object(), "x:Key", true)
            {
                Value = key
            };
            return new List<PropertyInformation>
            {
                prop
            };
        }

        private static List<PropertyDescriptor> GetAllProperties(object obj, Attribute[] attributes)
        {
            var propertiesToReturn = new List<PropertyDescriptor>();

            // keep looping until you don't have an AmbiguousMatchException exception
            // and you normally won't have an exception, so the loop will typically execute only once.
            var noException = false;
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

                    var t = obj.GetType();
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
                {
                    return true;
                }
            }

            return false;
        }

        public static Type GetNextTypeWithDefaultConstructor(Type type)
        {
            var t = type.BaseType;

            while (!HasDefaultConstructor(t))
            {
                t = t.BaseType;
            }

            return t;
        }

        private static void MergeProperties(IEnumerable newProperties, ICollection<PropertyDescriptor> allProperties)
        {
            foreach (var newProperty in newProperties)
            {
                var newPropertyDescriptor = newProperty as PropertyDescriptor;
                if (newPropertyDescriptor == null)
                {
                    continue;
                }

                if (!allProperties.Contains(newPropertyDescriptor))
                {
                    allProperties.Add(newPropertyDescriptor);
                }
            }
        }

        private bool isRunning;
        private bool ignoreUpdate;
        private string resourceKey;
        private static readonly Attribute[] getAllPropertiesAttributeFilter = { new PropertyFilterAttribute(PropertyFilterOptions.All) };

        public bool IsCollection()
        {
            var pattern = "^this\\[\\d+\\]$";
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
            var thisIndex = this.CollectionIndex();
            var objIndex = ((PropertyInformation)obj).CollectionIndex();
            if (thisIndex >= 0 && objIndex >= 0)
            {
                return thisIndex.CompareTo(objIndex);
            }

            return this.DisplayName.CompareTo(((PropertyInformation)obj).DisplayName);
        }
        #endregion

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
}
