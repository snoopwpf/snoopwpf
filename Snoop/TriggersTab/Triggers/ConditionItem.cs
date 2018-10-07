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

        private readonly AttachedPropertySlot attachedPropertySlot;

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

            this.BindCurrentValue(conditionContainer, this.dependencyPropertyDescriptor.DependencyProperty);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConditionItem" /> class.
        /// </summary>
        public ConditionItem(BindingBase conditionBinding, DependencyObject conditionContainer, object targetValue)
            : this(conditionContainer, targetValue, BindingDisplayHelper.BuildBindingDescriptiveString(conditionBinding))
        {
            this.conditionBinding = conditionBinding;

            this.attachedPropertySlot = AttachedPropertyManager.GetAndBindAttachedPropertySlot(this.conditionContainer, this.conditionBinding);            
            
            this.BindCurrentValue(conditionContainer, this.attachedPropertySlot.DependencyProperty);
        }

        public ConditionItem(DependencyObject conditionContainer, object targetValue, string displayName)
        {
            this.conditionContainer = conditionContainer ?? throw new ArgumentNullException(nameof(conditionContainer), "Condition container must not be null.");
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
                if (BindingOperations.IsDataBound(this, CurrentValueProperty) == false)
                {
                    return string.Empty;
                }

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
                return $"{this.DisplayName} == {this.TargetValue} TargetValue: {this.StringValue}";
            }
        }

        public string DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(this.SourceName)
                           ? this.displayName
                           : $"{this.displayName} ({this.SourceName})";
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
                              Path = new PropertyPath("(0)", dependencyProperty),
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

        #region IDisposable Members

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, CurrentValueProperty);

            if (this.attachedPropertySlot != null)
            {
                this.attachedPropertySlot.Dispose();
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
            this.StateChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}