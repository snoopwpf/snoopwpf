// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Threading;
    using JetBrains.Annotations;
    using Snoop.Converters;
    using Snoop.Infrastructure.Helpers;

    public class PropertyInformation : DependencyObject, IComparable, INotifyPropertyChanged
    {
        private static readonly Attribute[] getAllPropertiesAttributeFilter = { new PropertyFilterAttribute(PropertyFilterOptions.All) };

        private readonly object? component;
        private readonly bool isCopyable;
        private bool breakOnChange;
        private bool hasChangedRecently;
        private ValueSource valueSource;

        private string bindingError = string.Empty;
        private readonly PropertyDescriptor? property;
        private readonly string displayName;
        private bool isLocallySet;

        private bool isInvalidBinding;
        private int index;

        private bool isDatabound;

        private bool isRunning;
        private bool ignoreUpdate;

        /// <summary>
        /// Normal constructor used when constructing PropertyInformation objects for properties.
        /// </summary>
        /// <param name="target">target object being shown in the property grid</param>
        /// <param name="property">the property around which we are constructing this PropertyInformation object</param>
        /// <param name="propertyName">the property name for the property that we use in the binding in the case of a non-dependency property</param>
        /// <param name="propertyDisplayName">the display name for the property that goes in the name column</param>
        public PropertyInformation(object target, PropertyDescriptor? property, string propertyName, string propertyDisplayName)
        {
            this.Target = target;
            this.property = property;
            this.displayName = propertyDisplayName;

            if (property is not null)
            {
                // create a data binding between the actual property value on the target object
                // and the Value dependency property on this PropertyInformation object
                Binding binding;
                var dp = this.DependencyProperty;
                if (dp is not null)
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
        /// <param name="value">the value</param>
        public PropertyInformation(object target, object? component, string displayName, object? value, bool isCopyable = false)
            : this(target, null, displayName, displayName)
        {
            this.component = component;
            this.isCopyable = isCopyable;
            this.isRunning = false;
            this.Value = value;
            this.isRunning = true;
        }

        public void Teardown()
        {
            this.isRunning = false;
            BindingOperations.ClearAllBindings(this);
        }

        public object? Target { get; }

        public object? Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(PropertyInformation),
                new PropertyMetadata(OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

                if (this.changeTimer is null)
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

        private void HandleChangeExpiry(object? sender, EventArgs e)
        {
            this.changeTimer?.Stop();
            this.changeTimer = null;

            this.HasChangedRecently = false;
        }

        private DispatcherTimer? changeTimer;

        public string StringValue
        {
            get
            {
                var value = this.Value;
                if (value is not null)
                {
                    return value.ToString() ?? string.Empty;
                }

                return string.Empty;
            }

            set
            {
#pragma warning disable WPF0036 // Avoid side effects in CLR accessors.
                if (this.property is null)
                {
                    // if this is a PropertyInformation object constructed for an item in a collection
                    // then just return, since setting the value via a string doesn't make sense.
                    return;
                }
#pragma warning restore WPF0036 // Avoid side effects in CLR accessors.

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

        public string? ResourceKey
        {
            get
            {
                var value = this.Value;
                if (value is null
                    || this.property is null)
                {
                    return null;
                }

                string? resourceKey = null;

                if (this.Target is DependencyObject dependencyObject)
                {
                    // Cache the resource key for this item if not cached already. This could be done for more types, but would need to optimize perf.
                    if (this.TypeMightHaveResourceKey(this.property.PropertyType))
                    {
                        var resourceItem = value;
                        resourceKey = ResourceKeyCache.GetKey(resourceItem);

                        if (string.IsNullOrEmpty(resourceKey))
                        {
                            resourceKey = ResourceDictionaryKeyHelpers.GetKeyOfResourceItem(dependencyObject, resourceItem);
                            ResourceKeyCache.Cache(resourceItem, resourceKey);
                        }

                        Debug.Assert(resourceKey is not null, "resourceKey is not null");
                    }
                }

                return resourceKey;
            }
        }

        public string DescriptiveValue
        {
            get
            {
                var value = this.Value;
                if (value is null)
                {
                    return string.Empty;
                }

                var stringValue = value.ToString() ?? string.Empty;

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
                    else if (this.property is not null &&
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
                if (this.property is not null &&
                    this.property.PropertyType == typeof(Brush) &&
                    stringValue.Equals("#00FFFFFF"))
                {
                    stringValue = "Transparent";
                }

                if (this.Target is DependencyObject)
                {
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

        private bool TypeMightHaveResourceKey(Type type)
        {
            return type == typeof(Style)
                   || type == typeof(ControlTemplate)
                   || type == typeof(Color)
                   || type == typeof(Brush);
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

        public Type? ComponentType
        {
            get
            {
                if (this.property is null)
                {
                    // if this is a PropertyInformation object constructed for an item in a collection
                    // then this.property will be null, but this.component will contain the collection.
                    // use this object to return the type of the collection for the ComponentType.
                    return this.component?.GetType();
                }
                else
                {
                    return this.property.ComponentType;
                }
            }
        }

        public Type PropertyType
        {
            get
            {
                if (this.property is null)
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
                if (this.Value is not null)
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

        public PropertyDescriptor? Property
        {
            get { return this.property; }
        }

        public string DisplayName
        {
            get { return this.displayName; }
        }

        public bool IsCollectionEntry { get; private set; }

        public object? CollectionEntryIndexOrKey { get; private set; }

        public bool IsInvalidBinding
        {
            get { return this.isInvalidBinding; }
        }

        public bool IsLocallySet
        {
            get { return this.isLocallySet; }
        }

        public bool IsValueChangedByUser { get; set; }

        public bool CanEdit
        {
            get
            {
                if (this.property is null)
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

        public bool IsOdd
        {
            get { return this.index % 2 == 1; }
        }

        public BindingBase? Binding
        {
            get
            {
                var dp = this.DependencyProperty;
                if (dp is not null
                    && this.Target is DependencyObject d)
                {
                    return BindingOperations.GetBindingBase(d, dp);
                }

                return null;
            }
        }

        public BindingExpressionBase? BindingExpression
        {
            get
            {
                var dp = this.DependencyProperty;
                if (dp is not null
                    && this.Target is DependencyObject d)
                {
                    return BindingOperations.GetBindingExpressionBase(d, dp);
                }

                return null;
            }
        }

        public PropertyFilter? Filter
        {
            get { return this.filter; }

            set
            {
                this.filter = value;

                this.OnPropertyChanged(nameof(this.IsVisible));
            }
        }

        private PropertyFilter? filter;

        public bool BreakOnChange
        {
            get { return this.breakOnChange; }

            set
            {
                this.breakOnChange = value;
                this.OnPropertyChanged(nameof(this.BreakOnChange));
            }
        }

        public bool HasChangedRecently
        {
            get { return this.hasChangedRecently; }

            set
            {
                this.hasChangedRecently = value;
                this.OnPropertyChanged(nameof(this.HasChangedRecently));
            }
        }

        public ValueSource ValueSource
        {
            get { return this.valueSource; }
        }

        public bool IsVisible
        {
            get { return this.filter?.Show(this) != false; }
        }

        public void Clear()
        {
            var dp = this.DependencyProperty;
            if (dp is not null
                && this.Target is DependencyObject d)
            {
                d.ClearValue(dp);
            }
        }

        /// <summary>
        /// Returns the DependencyProperty identifier for the property that this PropertyInformation wraps.
        /// If the wrapped property is not a DependencyProperty, null is returned.
        /// </summary>
        public DependencyProperty? DependencyProperty
        {
            get
            {
                if (this.property is not null)
                {
                    // in order to be a DependencyProperty, the object must first be a regular property,
                    // and not an item in a collection.

                    var dpd = DependencyPropertyDescriptor.FromProperty(this.property);
                    if (dpd is not null)
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
            this.bindingError = string.Empty;
            this.isDatabound = false;

            var dp = this.DependencyProperty;
            var d = this.Target as DependencyObject;

            if (SnoopModes.MultipleDispatcherMode
                && d is not null
                && d.Dispatcher != this.Dispatcher)
            {
                return;
            }

            if (dp is not null
                && d is not null)
            {
                //Debugger.Launch();
                if (d.ReadLocalValue(dp) != DependencyProperty.UnsetValue)
                {
                    this.isLocallySet = true;
                }

                var expression = BindingOperations.GetBindingExpressionBase(d, dp);
                if (expression is not null)
                {
                    this.isDatabound = true;

                    if (expression.HasError
                        || (expression.Status != BindingStatus.Active && !(expression is PriorityBindingExpression)))
                    {
                        this.isInvalidBinding = true;
                    }
                }

                this.valueSource = DependencyPropertyHelper.GetValueSource(d, dp);
            }

            this.OnPropertyChanged(nameof(this.IsLocallySet));
            this.OnPropertyChanged(nameof(this.IsInvalidBinding));
            this.OnPropertyChanged(nameof(this.BindingError));
            this.OnPropertyChanged(nameof(this.StringValue));
            this.OnPropertyChanged(nameof(this.ResourceKey));
            this.OnPropertyChanged(nameof(this.DescriptiveValue));
            this.OnPropertyChanged(nameof(this.IsDatabound));
            this.OnPropertyChanged(nameof(this.IsExpression));
            this.OnPropertyChanged(nameof(this.IsAnimated));
            this.OnPropertyChanged(nameof(this.ValueSource));
        }

        public void UpdateBindingError()
        {
            if (this.IsDatabound == false
                || this.IsInvalidBinding == false)
            {
                return;
            }

            var dp = this.DependencyProperty;
            var d = this.Target as DependencyObject;

            if (SnoopModes.MultipleDispatcherMode
                && d is not null
                && d.Dispatcher != this.Dispatcher)
            {
                return;
            }

            if (dp is null
                || d is null)
            {
                return;
            }

            var expression = BindingOperations.GetBindingExpressionBase(d, dp);

            if (expression is null)
            {
                return;
            }

            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            var tracer = new TextWriterTraceListener(writer);
            var levelBefore = PresentationTraceSources.DataBindingSource.Switch.Level;
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.All;
            PresentationTraceSources.DataBindingSource.Listeners.Add(tracer);

            // reset binding to get the error message.
            this.ignoreUpdate = true;
            d.ClearValue(dp);
            BindingOperations.SetBinding(d, dp, expression.ParentBindingBase);
            this.ignoreUpdate = false;

            // this needs to happen on idle so that we can actually run the binding, which may occur asynchronously.
            this.RunInDispatcherAsync(
                () =>
                {
                    this.bindingError = builder.ToString();
                    this.OnPropertyChanged(nameof(this.BindingError));
                    PresentationTraceSources.DataBindingSource.Listeners.Remove(tracer);
                    writer.Close();
                    PresentationTraceSources.DataBindingSource.Switch.Level = levelBefore;
                }, DispatcherPriority.ApplicationIdle);
        }

        public static List<PropertyInformation> GetProperties(object? obj)
        {
            return GetProperties(obj, PertinentPropertyFilter.Filter);
        }

        public static List<PropertyInformation> GetProperties(object? obj, Func<object, PropertyDescriptor, bool> filter)
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

            // sort the properties before adding potential collection items
            properties.Sort();

            //delve path. also, issue 4919
            var extendedProps = GetExtendedProperties(obj);
            if (extendedProps is not null)
            {
                properties.InsertRange(0, extendedProps);
            }

            if (obj is ResourceDictionary resourceDictionary) // if the object is a ResourceDictionary, add the items in the collection as properties
            {
                foreach (var key in resourceDictionary.Keys)
                {
                    resourceDictionary.TryGetValue(key, out var item, out var exception);

                    if (item is not null
                        || key is not null)
                    {
                        var value = exception?.ToString() ?? item;
                        var info = new PropertyInformation(value ?? key!, resourceDictionary, "this[" + key + "]", value)
                        {
                            IsCollectionEntry = true,
                            CollectionEntryIndexOrKey = key
                        };

                        properties.Add(info);
                    }
                }
            }
            else if (obj is ICollection collection) // if the object is a collection, add the items in the collection as properties
            {
                var index = 0;
                foreach (var item in collection)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    var info = new PropertyInformation(item, collection, "this[" + index + "]", item)
                    {
                        IsCollectionEntry = true,
                        CollectionEntryIndexOrKey = index
                    };

                    index++;
                    properties.Add(info);
                }
            }

            return properties;
        }

        /// <summary>
        /// 4919 + Delve
        /// </summary>
        /// <returns></returns>
        private static IList<PropertyInformation>? GetExtendedProperties(object? obj)
        {
            if (obj is null)
            {
                return null;
            }

            if (ResourceKeyCache.Contains(obj))
            {
                var key = ResourceKeyCache.GetKey(obj);
                var prop = new PropertyInformation(key!, null, "x:Key", key!, isCopyable: true);
                return new List<PropertyInformation>
                {
                    prop
                };
            }

            if (obj is string
                || obj.GetType().IsValueType)
            {
                return new List<PropertyInformation> { new(obj, null, "ToString", obj, isCopyable: true) };
            }

            if (obj is AutomationPeer automationPeer)
            {
                var automationProperties = new List<PropertyInformation>
                    {
                        new(obj, null, "ClassName", automationPeer.GetClassName(), isCopyable: true),
                        new(obj, null, "Name", automationPeer.GetName(), isCopyable: true),
                        new(obj, null, "AcceleratorKey", automationPeer.GetAcceleratorKey(), isCopyable: true),
                        new(obj, null, "AccessKey", automationPeer.GetAccessKey(), isCopyable: true),
                        new(obj, null, "AutomationControlType", automationPeer.GetAutomationControlType(), isCopyable: true),
                        new(obj, null, "AutomationId", automationPeer.GetAutomationId(), isCopyable: true),
                        new(obj, null, "BoundingRectangle", automationPeer.GetBoundingRectangle(), isCopyable: true),
                        new(obj, null, "ClickablePoint", automationPeer.GetClickablePoint(), isCopyable: true),
                        new(obj, null, "HelpText", automationPeer.GetHelpText(), isCopyable: true),
                        new(obj, null, "ItemStatus", automationPeer.GetItemStatus(), isCopyable: true),
                        new(obj, null, "ItemType", automationPeer.GetItemType(), isCopyable: true),
                        new(obj, null, "LabeledBy", automationPeer.GetLabeledBy(), isCopyable: true),
                        #if !NET40
                        new(obj, null, "LiveSetting", automationPeer.GetLiveSetting(), isCopyable: true),
                        #endif
                        new(obj, null, "LocalizedControlType", automationPeer.GetLocalizedControlType(), isCopyable: true),
                        new(obj, null, "Orientation", automationPeer.GetOrientation(), isCopyable: true),
                    };

                var supportedPatterns = new List<string>();

                foreach (PatternInterface? patternInterface in Enum.GetValues(typeof(PatternInterface)))
                {
                    if (patternInterface is null)
                    {
                        continue;
                    }

                    if (automationPeer.GetPattern(patternInterface.Value) is not null)
                    {
                        supportedPatterns.Add(patternInterface.Value.ToString());
                    }
                }

                automationProperties.Add(new PropertyInformation(obj, null, "SupportedPatterns", string.Join(", ", supportedPatterns), isCopyable: true));

                return automationProperties;
            }

            return null;
        }

        private static List<PropertyDescriptor> GetAllProperties(object obj, Attribute[] attributes)
        {
            var propertiesToReturn = new List<PropertyDescriptor>();

            object? currentObj = obj;

            // keep looping until you don't have an AmbiguousMatchException exception
            // and you normally won't have an exception, so the loop will typically execute only once.
            var noException = false;
            while (!noException && currentObj is not null)
            {
                try
                {
                    // try to get the properties using the GetProperties method that takes an instance
                    var properties = TypeDescriptor.GetProperties(currentObj, attributes);
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

                    var t = currentObj.GetType();
                    var properties = TypeDescriptor.GetProperties(t, attributes);

                    MergeProperties(properties, propertiesToReturn);

                    var nextBaseTypeWithDefaultConstructor = GetNextTypeWithDefaultConstructor(t);

                    if (nextBaseTypeWithDefaultConstructor is null)
                    {
                        break;
                    }

                    currentObj = Activator.CreateInstance(nextBaseTypeWithDefaultConstructor)!;
                }
            }

            return propertiesToReturn;
        }

        public static bool HasDefaultConstructor(Type? type)
        {
            var constructors = type?.GetConstructors();

            if (constructors is null)
            {
                return false;
            }

            foreach (var constructor in constructors)
            {
                if (constructor.GetParameters().Length == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static Type? GetNextTypeWithDefaultConstructor(Type type)
        {
            var t = type.BaseType;

            while (!HasDefaultConstructor(t))
            {
                t = t?.BaseType;
            }

            return t;
        }

        private static void MergeProperties(IEnumerable newProperties, ICollection<PropertyDescriptor> allProperties)
        {
            foreach (var newProperty in newProperties)
            {
                var newPropertyDescriptor = newProperty as PropertyDescriptor;
                if (newPropertyDescriptor is null)
                {
                    continue;
                }

                if (!allProperties.Contains(newPropertyDescriptor))
                {
                    allProperties.Add(newPropertyDescriptor);
                }
            }
        }

        private int CollectionIndex()
        {
            if (this.IsCollectionEntry
                && this.CollectionEntryIndexOrKey is int collectionEntryIndex)
            {
                return collectionEntryIndex;
            }

            return -1;
        }

        #region IComparable Members

        public int CompareTo(object? obj)
        {
            var thisIndex = this.CollectionIndex();
            var other = obj as PropertyInformation;

            if (other is not null)
            {
                var objIndex = other.CollectionIndex();
                if (thisIndex >= 0
                    && objIndex >= 0)
                {
                    return thisIndex.CompareTo(objIndex);
                }
            }

            return string.Compare(this.DisplayName, other?.DisplayName, StringComparison.Ordinal);
        }

        #endregion

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
