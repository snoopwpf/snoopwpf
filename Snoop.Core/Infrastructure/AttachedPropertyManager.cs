namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

public static class AttachedPropertyManager
{
    public static AttachedPropertySlot? GetAndBindAttachedPropertySlot(DependencyObject target, BindingBase binding)
    {
        var nextFreeAttachedProperty = GetNextFreeAttachedProperty(target);

        if (nextFreeAttachedProperty is null)
        {
            return null;
        }

        return new AttachedPropertySlot(target, nextFreeAttachedProperty, binding);
    }

    private static DependencyProperty? GetNextFreeAttachedProperty(DependencyObject target)
    {
        for (var i = 0; i < int.MaxValue; i++)
        {
            var attachedProperty = GetAttachedPropertyFromIndex(i);
            var localValue = target.ReadLocalValue(attachedProperty);

            if (localValue == DependencyProperty.UnsetValue)
            {
                return attachedProperty;
            }
        }

        return null;
    }

    private static DependencyProperty GetAttachedPropertyFromIndex(int index)
    {
        if (attachedDependencyProperties.TryGetValue(index, out var attachedProperty) == false)
        {
            attachedProperty = DependencyProperty.RegisterAttached("Snoop_Runtime_AttachedProperty_" + index, typeof(object), typeof(AttachedPropertyManager), new FrameworkPropertyMetadata(null));
            attachedDependencyProperties.Add(index, attachedProperty);
        }

        return attachedProperty;
    }

    private static readonly Dictionary<int, DependencyProperty> attachedDependencyProperties = new(64);
}

public class AttachedPropertySlot : IDisposable
{
    public AttachedPropertySlot(DependencyObject target, DependencyProperty dependencyProperty, BindingBase binding)
    {
        this.Target = target;
        this.DependencyProperty = dependencyProperty;
        this.Binding = binding;

        BindingOperations.SetBinding(this.Target, this.DependencyProperty, binding);
    }

    public DependencyObject Target { get; }

    public DependencyProperty DependencyProperty { get; }

    public BindingBase Binding { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        BindingOperations.ClearBinding(this.Target, this.DependencyProperty);
    }
}