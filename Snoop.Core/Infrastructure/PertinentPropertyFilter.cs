// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System.ComponentModel;
    using System.Windows;

    public static class PertinentPropertyFilter
    {
        public static bool Filter(object target, PropertyDescriptor property)
        {
            var frameworkElement = target as FrameworkElement;

            if (frameworkElement is null)
            {
                return true;
            }

            var attachedPropertyForChildren = (AttachedPropertyBrowsableForChildrenAttribute?)property.Attributes[typeof(AttachedPropertyBrowsableForChildrenAttribute)];

            if (attachedPropertyForChildren is not null)
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(property);
                if (dpd is null)
                {
                    return false;
                }

                var currentElement = frameworkElement;
                do
                {
                    currentElement = currentElement.Parent as FrameworkElement;
                    if (currentElement is not null
                        && dpd.DependencyProperty.OwnerType.IsInstanceOfType(currentElement))
                    {
                        return true;
                    }
                }
                while (attachedPropertyForChildren.IncludeDescendants && currentElement is not null);

                return false;
            }

            var attachedPropertyForType = (AttachedPropertyBrowsableForTypeAttribute?)property.Attributes[typeof(AttachedPropertyBrowsableForTypeAttribute)];

            if (attachedPropertyForType is not null)
            {
                // when using [AttachedPropertyBrowsableForType(typeof(IMyInterface))] and IMyInterface is not a DependencyObject, Snoop crashes.
                // see http://snoopwpf.codeplex.com/workitem/6712

                if (typeof(DependencyObject).IsAssignableFrom(attachedPropertyForType.TargetType))
                {
                    var doType = DependencyObjectType.FromSystemType(attachedPropertyForType.TargetType);
                    if (doType is not null
                        && doType.IsInstanceOfType(frameworkElement))
                    {
                        return true;
                    }
                }

                return false;
            }

            var attachedPropertyForAttribute = (AttachedPropertyBrowsableWhenAttributePresentAttribute?)property.Attributes[typeof(AttachedPropertyBrowsableWhenAttributePresentAttribute)];

            if (attachedPropertyForAttribute is not null)
            {
                var dependentAttribute = TypeDescriptor.GetAttributes(target)[attachedPropertyForAttribute.AttributeType];
                if (dependentAttribute is not null)
                {
                    return !dependentAttribute.IsDefaultAttribute();
                }

                return false;
            }

            return true;
        }
    }
}