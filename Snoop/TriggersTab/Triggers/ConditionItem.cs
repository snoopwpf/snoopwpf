namespace Snoop.TriggersTab.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Data;
    using Snoop.Infrastructure;

    public class ConditionItem : DependencyObject, IDisposable, INotifyPropertyChanged
    {
        private readonly string displayName;
        private readonly BindingBase conditionBinding;
        private readonly DependencyObject conditionContainer;
        private readonly object targetValue;

        private readonly int attachedPropertyIndex = -1;

        private readonly DependencyPropertyDescriptor dependencyPropertyDescriptor;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConditionItem" /> class.
        /// </summary>
        public ConditionItem(DependencyProperty dependencyProperty, DependencyObject conditionContainer, object targetValue)
            : this(DependencyPropertyDescriptor.FromProperty(dependencyProperty, conditionContainer.GetType()), conditionContainer, targetValue)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConditionItem" /> class.
        /// </summary>
        public ConditionItem(DependencyPropertyDescriptor propertyDescriptor, DependencyObject conditionContainer, object targetValue)
            : this(conditionContainer, targetValue, propertyDescriptor.DisplayName)
        {
            this.dependencyPropertyDescriptor = propertyDescriptor;

            this.BindCurrentValue(conditionContainer, propertyDescriptor.DependencyProperty);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConditionItem" /> class.
        /// </summary>
        public ConditionItem(BindingBase conditionBinding, DependencyObject conditionContainer, object targetValue)
            : this(conditionContainer, targetValue, BindingDisplayHelper.BuildBindingDescriptiveString(conditionBinding))
        {
            this.conditionBinding = conditionBinding;

            this.attachedPropertyIndex = this.GetNextFreeAttachedPropertyIndex();
            var attachedPropertyFromIndex = GetAttachedPropertyFromIndex(this.attachedPropertyIndex);
            BindingOperations.SetBinding(this.conditionContainer, attachedPropertyFromIndex, this.conditionBinding);
            
            this.BindCurrentValue(conditionContainer, attachedPropertyFromIndex);
        }

        private ConditionItem(DependencyObject conditionContainer, object targetValue, string displayName)
        {
            if (conditionContainer == null)
            {
                throw new ArgumentNullException("conditionContainer", "Instance must not be null.");
            }

            this.conditionContainer = conditionContainer;
            this.targetValue = targetValue;

            this.displayName = displayName;
        }

        public object CurrentValue
        {
            get { return this.GetValue(ConditionItem.CurrentValueProperty); }
            set { this.SetValue(ConditionItem.CurrentValueProperty, value); }
        }

        public static readonly DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(object), typeof(ConditionItem), new PropertyMetadata(HandleCurrentValueChanged));        

        private static void HandleCurrentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ConditionItem)d).OnCurrentValueChanged();
        }

        private void OnCurrentValueChanged()
        {
            if (SnoopModes.MultipleDispatcherMode
                && this.conditionContainer.Dispatcher != this.Dispatcher)
            {
                return;
            }

            this.OnPropertyChanged("StringValue");
            this.OnPropertyChanged("IsActive");
            this.OnPropertyChanged("CurrentValue");
            this.OnPropertyChanged("Condition");

            this.NotifyStateChanged();
        }

        public string SourceName { get; set; }

        public string StringValue
        {
            get
            {
                var value = this.CurrentValue;
                if (value != null)
                {
                    return value.ToString();
                }

                return "{x:Null}";
            }
        }

        /// <summary>
        ///     Gets if the condition is currently active
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (this.CurrentValue != null
                    && this.targetValue != null
                    && this.CurrentValue.GetType() != this.targetValue.GetType())
                {
                    var converter = TypeDescriptor.GetConverter(this.CurrentValue.GetType());
                    if (converter.CanConvertFrom(this.targetValue.GetType()))
                    {
                        try
                        {
                            return this.CurrentValue.Equals(converter.ConvertFrom(this.targetValue));
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                }

                if (this.targetValue == null)
                {
                    return this.CurrentValue == null;
                }

                return this.targetValue.Equals(this.CurrentValue);
            }
        }

        public string Condition
        {
            get
            {
                return string.Format("{0} == {1} TargetValue: {2}", this.DisplayName, this.TargetValue, this.StringValue);
            }
        }

        public string DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(this.SourceName)
                           ? this.displayName
                           : string.Format("{0} ({1})", this.displayName, this.SourceName);
            }
        }

        public string TargetValue
        {
            get
            {
                if (this.targetValue == null)
                {
                    return "null";
                }

                return this.targetValue.ToString();
            }
        }

        private void BindCurrentValue(object instance, DependencyProperty dependencyProperty)
        {
            // create a data binding between the actual property value on the target object
            // and the Value dependency property on this PropertyInformation object
            var bindingForCurrentValue = new Binding
                          {
                              Path = new PropertyPath("(0)", new object[] { dependencyProperty }),
                              Source = instance,
                              Mode = BindingMode.OneWay
                          };

            try
            {
                BindingOperations.SetBinding(this, ConditionItem.CurrentValueProperty, bindingForCurrentValue);
            }
            catch (Exception)
            {
                // cplotts note:
                // warning: i saw a problem get swallowed by this empty catch (Exception) block.
                // in other words, this empty catch block could be hiding some potential future errors.
            }
        }

        private int GetNextFreeAttachedPropertyIndex()
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                var attachedProperty = GetAttachedPropertyFromIndex(i);
                var localValue = ((DependencyObject)this.conditionContainer).ReadLocalValue(attachedProperty);

                if (localValue == DependencyProperty.UnsetValue)
                {
                    return i;
                }
            }

            return -1;
        }

        private static DependencyProperty GetAttachedPropertyFromIndex(int index)
        {
            DependencyProperty attachedProperty;
            if (attachedDependencyProperties.TryGetValue(index, out attachedProperty) == false)
            {
                attachedProperty = DependencyProperty.RegisterAttached("ConditionItem_AttachedProperty_" + index, typeof(object), typeof(FrameworkElement), new FrameworkPropertyMetadata(null));
                attachedDependencyProperties.Add(index, attachedProperty);
            }

            return attachedProperty;
        }

        private static readonly Dictionary<int, DependencyProperty> attachedDependencyProperties = new Dictionary<int, DependencyProperty>(64);        

        #region IDisposable Members

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, CurrentValueProperty);

            if (this.attachedPropertyIndex != -1)
            {
                BindingOperations.ClearBinding(this.conditionContainer, GetAttachedPropertyFromIndex(this.attachedPropertyIndex));
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            Debug.Assert(this.GetType().GetProperty(propertyName) != null);

            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        /// <summary>
        ///     Occurs when the state of the condition changed.
        /// </summary>
        public event EventHandler StateChanged;

        #region Private Helpers

        private void NotifyStateChanged()
        {
            var handler = this.StateChanged;
            if (handler != null)
            {
                handler.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}