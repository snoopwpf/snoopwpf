// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

public static class PertinentPropertyFilter
{
    public static bool Filter(object target, PropertyDescriptor property)
    {
        if (target is not DependencyObject dependencyObject)
        {
            return true;
        }

        if (DependencyPropertyDescriptor.FromProperty(property) is not { IsAttached: true })
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

            // Check logical tree
            {
                var currentElement = dependencyObject;
                do
                {
                    currentElement = LogicalTreeHelper.GetParent(currentElement);
                    if (currentElement is not null
                        && dpd.DependencyProperty.OwnerType.IsInstanceOfType(currentElement))
                    {
                        return true;
                    }
                }
                while (attachedPropertyForChildren.IncludeDescendants && currentElement is not null);
            }

            // Check visual tree
            if (dependencyObject is Visual or Visual3D)
            {
                var currentElement = dependencyObject;
                do
                {
                    currentElement = VisualTreeHelper.GetParent(currentElement);
                    if (currentElement is not null
                        && dpd.DependencyProperty.OwnerType.IsInstanceOfType(currentElement))
                    {
                        return true;
                    }
                }
                while (attachedPropertyForChildren.IncludeDescendants && currentElement is not null);
            }

            return false;
        }

        var browsableForTypeAttributes = property.Attributes.OfType<AttachedPropertyBrowsableForTypeAttribute>();

        {
            var hadAttribute = false;
            foreach (var attachedPropertyForType in browsableForTypeAttributes)
            {
                hadAttribute = true;

                if (attachedPropertyForType.TargetType.IsInstanceOfType(target))
                {
                    return true;
                }
            }

            if (hadAttribute)
            {
                return false;
            }
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