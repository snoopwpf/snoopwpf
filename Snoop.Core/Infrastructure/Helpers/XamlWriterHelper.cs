namespace Snoop.Infrastructure.Helpers
{
    using System.Linq;
    using System.Xml.Linq;
    using XamlWriter = System.Windows.Markup.XamlWriter;

    public static class XamlWriterHelper
    {
        public static string GetXamlAsString(object obj)
        {
            var savedXaml = XamlWriter.Save(obj);
            
            var xamlWithoutNamespaces = RemoveNamespacesFromXml(savedXaml);

            return xamlWithoutNamespaces.ToString(SaveOptions.OmitDuplicateNamespaces);
        }

        public static XElement GetXamlAsXElement(object obj)
        {
            var savedXaml = XamlWriter.Save(obj);

            var xamlWithoutNamespaces = RemoveNamespacesFromXml(savedXaml);

            return xamlWithoutNamespaces;
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