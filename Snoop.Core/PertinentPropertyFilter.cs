// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Windows;

namespace Snoop
{
	public class PertinentPropertyFilter
	{
		public PertinentPropertyFilter(object target)
		{
			this.target = target;
			this.element = this.target as FrameworkElement;
		}


		public bool Filter(PropertyDescriptor property)
		{
			if (this.element == null)
				return true;

			// Filter the 20 stylistic set properties that I've never seen used.
			if (property.Name.Contains("Typography.StylisticSet"))
				return false;

			AttachedPropertyBrowsableForChildrenAttribute attachedPropertyForChildren = (AttachedPropertyBrowsableForChildrenAttribute)property.Attributes[typeof(AttachedPropertyBrowsableForChildrenAttribute)];
			AttachedPropertyBrowsableForTypeAttribute attachedPropertyForType = (AttachedPropertyBrowsableForTypeAttribute)property.Attributes[typeof(AttachedPropertyBrowsableForTypeAttribute)];
			AttachedPropertyBrowsableWhenAttributePresentAttribute attachedPropertyForAttribute = (AttachedPropertyBrowsableWhenAttributePresentAttribute)property.Attributes[typeof(AttachedPropertyBrowsableWhenAttributePresentAttribute)];

			if (attachedPropertyForChildren != null)
			{
				DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property);
				if (dpd == null)
					return false;

				FrameworkElement element = this.element;
				do
				{
					element = element.Parent as FrameworkElement;
					if (element != null && dpd.DependencyProperty.OwnerType.IsInstanceOfType(element))
						return true;
				}
				while (attachedPropertyForChildren.IncludeDescendants && element != null);
				return false;
			}
			else if (attachedPropertyForType != null)
			{
				// when using [AttachedPropertyBrowsableForType(typeof(IMyInterface))] and IMyInterface is not a DependencyObject, Snoop crashes.
				// see http://snoopwpf.codeplex.com/workitem/6712

				if (attachedPropertyForType.TargetType.IsSubclassOf(typeof(DependencyObject)))
				{
					DependencyObjectType doType = DependencyObjectType.FromSystemType(attachedPropertyForType.TargetType);
					if (doType != null && doType.IsInstanceOfType(this.element))
						return true;
				}

				return false;
			}
			else if (attachedPropertyForAttribute != null)
			{
				Attribute dependentAttribute = TypeDescriptor.GetAttributes(this.target)[attachedPropertyForAttribute.AttributeType];
				if (dependentAttribute != null)
					return !dependentAttribute.IsDefaultAttribute();
				return false;
			}

			return true;
		}


		private object target;
		private FrameworkElement element;
	}
}
