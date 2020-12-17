namespace Snoop.Views.TriggersTab.Triggers
{
    using System;
    using System.ComponentModel;
    using System.Text;
    using System.Windows;
    using System.Windows.Data;
    using Snoop.Converters;
    using Snoop.Infrastructure;
    using Snoop.Infrastructure.Helpers;

    /// <summary>
    ///     Abstraction model of a <see cref="Setter" />
    /// </summary>
    public class SetterItem : IDisposable
    {
        private AttachedPropertySlot? attachedPropertySlot;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SetterItem" /> class.
        /// </summary>
        public SetterItem(SetterBase setterBase, DependencyObject element)
        {
            this.Property = "Unknown";

            this.Setter = setterBase as Setter;
            if (this.Setter is null)
            {
                return;
            }

            this.Property = this.Setter.Property.Name;

            var propertyDescriptor = DependencyPropertyDescriptor.FromProperty(this.Setter.Property, element.GetType());
            var targetForPropertyInformation = TemplateHelper.GetChildFromTemplateIfNeeded(element, this.Setter.TargetName);

            if (targetForPropertyInformation is null)
            {
                return;
            }

            var binding = this.CreateBinding(targetForPropertyInformation, propertyDescriptor, this.Setter);

            if (binding is null == false)
            {
                var propertyInformation = new PropertyInformation(targetForPropertyInformation, propertyDescriptor, binding, GetDisplayName(this.Setter));

                this.Value = propertyInformation;
            }
        }

        public Setter? Setter { get; }

        /// <summary>
        ///     Gets if the value is overridden by a descending style or template
        /// </summary>
        public bool IsOverridden { get; set; }

        /// <summary>
        ///     Gets the name of the property.
        /// </summary>
        public string? Property { get; }

        /// <summary>
        ///     Gets the value.
        /// </summary>
        public PropertyInformation? Value { get; }

        private static string GetDisplayName(Setter setter)
        {
            var sb = new StringBuilder(setter.Property.Name);

            if (string.IsNullOrEmpty(setter.TargetName) == false)
            {
                sb.AppendFormat(" ({0})", setter.TargetName);
            }

            sb.Append(":");

            return sb.ToString();
        }

        private Binding? CreateBinding(object target, DependencyPropertyDescriptor property, Setter setter)
        {
            if (setter.Value is BindingBase
                && target is DependencyObject)
            {
                this.attachedPropertySlot = AttachedPropertyManager.GetAndBindAttachedPropertySlot((DependencyObject)target, (BindingBase)setter.Value);

                if (this.attachedPropertySlot is null)
                {
                    return null;
                }

                var binding = new Binding
                {
                    Path = new PropertyPath("(0)", this.attachedPropertySlot.DependencyProperty),
                    Source = target,
                    Mode = BindingMode.OneWay
                };

                return binding;
            }
            else
            {
                var binding = new Binding(nameof(System.Windows.Setter.Value))
                {
                    Source = setter,
                    Mode = property.IsReadOnly
                                             ? BindingMode.OneWay
                                             : BindingMode.TwoWay,
                    Converter = new DynamicResourceToValueConverter(target)
                };

                return binding;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.Value != null)
            {
                this.Value.Teardown();
            }

            if (this.attachedPropertySlot != null)
            {
                this.attachedPropertySlot.Dispose();
            }
        }
    }
}