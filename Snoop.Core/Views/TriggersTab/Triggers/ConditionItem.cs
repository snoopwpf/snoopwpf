namespace Snoop.Views.TriggersTab.Triggers;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using JetBrains.Annotations;
using Snoop.Infrastructure;
using Snoop.Infrastructure.Helpers;

public class ConditionItem : DependencyObject, IDisposable, INotifyPropertyChanged
{
    private readonly string displayName;
    private readonly BindingBase? conditionBinding;
    private readonly DependencyObject conditionContainer;
    private readonly object? targetValue;

    private readonly AttachedPropertySlot? attachedPropertySlot;

    private readonly DependencyPropertyDescriptor? dependencyPropertyDescriptor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConditionItem" /> class.
    /// </summary>
    public ConditionItem(DependencyProperty dependencyProperty, DependencyObject conditionContainer, object? targetValue)
        : this(dependencyProperty, GetDependencyPropertyDescriptor(dependencyProperty, conditionContainer), conditionContainer, targetValue)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConditionItem" /> class.
    /// </summary>
    public ConditionItem(DependencyProperty dependencyProperty, DependencyPropertyDescriptor propertyDescriptor, DependencyObject conditionContainer, object? targetValue)
        : this(conditionContainer, targetValue, GetDisplayName(dependencyProperty, propertyDescriptor))
    {
        this.dependencyPropertyDescriptor = propertyDescriptor;

        if (this.dependencyPropertyDescriptor is null)
        {
            this.HasError = true;
            this.Error = $"DependencyPropertyDescriptor for '{this.DisplayName}' could not be found.{Environment.NewLine}In case of an attached property this might be caused by a missing \"get\"-method for that property.";
        }

        this.BindCurrentValue(conditionContainer, dependencyProperty);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConditionItem" /> class.
    /// </summary>
    public ConditionItem(BindingBase conditionBinding, DependencyObject conditionContainer, object? targetValue)
        : this(conditionContainer, targetValue, BindingDisplayHelper.BuildBindingDescriptiveString(conditionBinding))
    {
        this.conditionBinding = conditionBinding;

        this.attachedPropertySlot = AttachedPropertyManager.GetAndBindAttachedPropertySlot(this.conditionContainer, this.conditionBinding);

        if (this.attachedPropertySlot is not null)
        {
            this.BindCurrentValue(conditionContainer, this.attachedPropertySlot.DependencyProperty);
        }
    }

    public ConditionItem(DependencyObject conditionContainer, object? targetValue, string displayName)
    {
        this.conditionContainer = conditionContainer ?? throw new ArgumentNullException(nameof(conditionContainer), "Condition container must not be null.");
        this.targetValue = targetValue;

        this.displayName = displayName;
    }

    public bool HasError { get; }

    public string? Error { get; }

    public object? CurrentValue
    {
        get { return this.GetValue(CurrentValueProperty); }
        set { this.SetValue(CurrentValueProperty, value); }
    }

    public static readonly DependencyProperty CurrentValueProperty =
        DependencyProperty.Register(nameof(CurrentValue), typeof(object), typeof(ConditionItem), new PropertyMetadata(OnCurrentValueChanged));

    private static void OnCurrentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

        this.OnPropertyChanged(nameof(this.StringValue));
        this.OnPropertyChanged(nameof(this.IsActive));
        this.OnPropertyChanged(nameof(this.CurrentValue));
        this.OnPropertyChanged(nameof(this.Condition));

        this.NotifyStateChanged();
    }

    public string? SourceName { get; set; }

    public string? StringValue
    {
        get
        {
            if (BindingOperations.IsDataBound(this, CurrentValueProperty) == false)
            {
                return string.Empty;
            }

            var value = this.CurrentValue;
            if (value is not null)
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
            if (this.CurrentValue is not null
                && this.targetValue is not null
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

            if (this.targetValue is null)
            {
                return this.CurrentValue is null;
            }

            return this.targetValue.Equals(this.CurrentValue);
        }
    }

    public string Condition
    {
        get
        {
            return $"{this.DisplayName} == {this.TargetValue} [Current value: {this.StringValue}]";
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

    public string? TargetValue
    {
        get
        {
            if (this.targetValue is null)
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
            BindingOperations.SetBinding(this, CurrentValueProperty, bindingForCurrentValue);
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

        this.attachedPropertySlot?.Dispose();
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

    /// <summary>
    ///     Occurs when the state of the condition changed.
    /// </summary>
    public event EventHandler? StateChanged;

    #region Private Helpers

    private void NotifyStateChanged()
    {
        this.StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static DependencyPropertyDescriptor GetDependencyPropertyDescriptor(DependencyProperty dependencyProperty, DependencyObject targetType)
    {
        return DependencyPropertyDescriptor.FromProperty(dependencyProperty, targetType.GetType());
    }

    private static string GetDisplayName(DependencyProperty dependencyProperty, DependencyPropertyDescriptor? propertyDescriptor)
    {
        if (propertyDescriptor is not null)
        {
            return propertyDescriptor.DisplayName;
        }

        return $"{dependencyProperty.OwnerType.Name}.{dependencyProperty.Name}";
    }

    #endregion
}