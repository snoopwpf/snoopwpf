namespace Snoop
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Data;
    using System.Windows.Markup;
    using System.Xml.Linq;

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

            var savedXaml = XamlWriter.Save(binding);

            var xamlWithoutNamespaces = RemoveNamespacesFromXml(savedXaml);

            foreach (var propertyName in propertyNames)
            {
                var attribute = xamlWithoutNamespaces.Attribute(propertyName);

                if (attribute != null)
                {
                    propertyValues.Add(string.Format("{0}={1}", propertyName, attribute.Value));
                }
            }

            return string.Join(",", propertyValues.ToArray());
        }

        public static XElement RemoveNamespacesFromXml(string input)
        {
            var xml = XElement.Parse(input);
            foreach (var xe in xml.DescendantsAndSelf())
            {
                // Stripping the namespace by setting the name of the element to it's localname only
                xe.Name = xe.Name.LocalName;
                // replacing all attributes with attributes that are not namespaces and their names are set to only the localname
                xe.ReplaceAttributes(from xattrib in xe.Attributes().Where(xa => !xa.IsNamespaceDeclaration) select new XAttribute(xattrib.Name.LocalName, xattrib.Value));
            }

            return xml;
        }
    }
}