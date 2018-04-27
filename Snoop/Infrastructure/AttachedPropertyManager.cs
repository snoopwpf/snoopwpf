namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Data;

    public static class AttachedPropertyManager
    {
        public static AttachedPropertySlot GetAndBindAttachedPropertySlot(DependencyObject target, BindingBase binding)
        {
            var nextFreeAttachedProperty = GetNextFreeAttachedProperty(target);

            return new AttachedPropertySlot(target, nextFreeAttachedProperty, binding);
        }

        private static DependencyProperty GetNextFreeAttachedProperty(DependencyObject target)
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
            DependencyProperty attachedProperty;
            if (attachedDependencyProperties.TryGetValue(index, out attachedProperty) == false)
            {
                attachedProperty = DependencyProperty.RegisterAttached("Snoop_Runtime_AttachedProperty_" + index, typeof(object), typeof(FrameworkElement), new FrameworkPropertyMetadata(null));
                attachedDependencyProperties.Add(index, attachedProperty);
            }

            return attachedProperty;
        }

        private static readonly Dictionary<int, DependencyProperty> attachedDependencyProperties = new Dictionary<int, DependencyProperty>(64);
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

        public DependencyObject Target { get; private set; }

        public DependencyProperty DependencyProperty { get; private set; }

        public BindingBase Binding { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            BindingOperations.ClearBinding(this.Target, this.DependencyProperty);
        }
    }
}