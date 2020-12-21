namespace Snoop.Infrastructure.Helpers
{
    using System.Collections.Generic;
    using System.Windows.Data;

    public static class BindingDisplayHelper
    {
        /// <summary>
        ///     Build up a string describing the Binding.  Path and ElementName (if present)
        /// </summary>
        public static string BuildBindingDescriptiveString(BindingBase binding)
        {
            return BuildBindingDescriptiveString(binding, "Path", "ElementName", "RelativeSource");
        }

        /// <summary>
        ///     Build up a string describing the Binding.  Path and ElementName (if present)
        /// </summary>
        public static string BuildBindingDescriptiveString(BindingBase binding, params string[] propertyNames)
        {
            var propertyValues = new List<string>(propertyNames.Length);

            var xaml = XamlWriterHelper.GetXamlAsXElement(binding).RemoveNamespaces();

            foreach (var propertyName in propertyNames)
            {
                var attribute = xaml.Attribute(propertyName);

                if (attribute is not null)
                {
                    propertyValues.Add($"{propertyName}={attribute.Value}");
                }
            }

            return string.Join(",", propertyValues.ToArray());
        }
    }
}