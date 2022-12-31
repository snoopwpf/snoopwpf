namespace Snoop.Infrastructure;

using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using Snoop.Data.Tree;

public static class TreeExporter
{
    public static void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, bool recurse = true)
    {
        new XMLTreeExporter().Export(treeItem, textWriter, filter, recurse);
    }
}

public class ExportOptions : DependencyObject
{
    public static readonly DependencyProperty TreeItemProperty = DependencyProperty.Register(
        nameof(TreeItem), typeof(TreeItem), typeof(ExportOptions), new PropertyMetadata(default(TreeItem)));

    public TreeItem? TreeItem
    {
        get { return (TreeItem?)this.GetValue(TreeItemProperty); }
        set { this.SetValue(TreeItemProperty, value); }
    }

    public static readonly DependencyProperty UseFilterProperty = DependencyProperty.Register(
        nameof(UseFilter), typeof(bool), typeof(ExportOptions), new PropertyMetadata(true));

    public bool UseFilter
    {
        get { return (bool)this.GetValue(UseFilterProperty); }
        set { this.SetValue(UseFilterProperty, value); }
    }

    public static readonly DependencyProperty RecurseProperty = DependencyProperty.Register(
        nameof(Recurse), typeof(bool), typeof(ExportOptions), new PropertyMetadata(false));

    public bool Recurse
    {
        get { return (bool)this.GetValue(RecurseProperty); }
        set { this.SetValue(RecurseProperty, value); }
    }
}

public class XMLTreeExporter
{
    public void Export(TreeItem treeItem, TextWriter textWriter, PropertyFilter? filter, bool recurse)
    {
        var writerSettings = new XmlWriterSettings
        {
            Encoding = textWriter.Encoding,
            Indent = true,
            NewLineOnAttributes = false
        };

        using var xmlWriter = XmlWriter.Create(textWriter, writerSettings);
        xmlWriter.WriteStartDocument(true);
        this.ExportItem(treeItem, xmlWriter, filter, recurse);
        xmlWriter.WriteEndDocument();
    }

    private void ExportItem(TreeItem treeItem, XmlWriter xmlWriter, PropertyFilter? filter, bool recurse)
    {
        xmlWriter.WriteStartElement("node");
        xmlWriter.WriteAttributeString("name", treeItem.Name);
        xmlWriter.WriteAttributeString("displayName", treeItem.DisplayName);
        xmlWriter.WriteAttributeString("targetType", treeItem.TargetType.FullName!);

        var propertyInformations = PropertyInformation.GetProperties(treeItem.Target);

        if (propertyInformations.Any())
        {
            xmlWriter.WriteStartElement("properties");

            foreach (var propertyInformation in propertyInformations)
            {
                if (filter is not null
                    && filter.ShouldShow(propertyInformation) == false)
                {
                    continue;
                }

                xmlWriter.WriteStartElement("property");
                xmlWriter.WriteAttributeString("displayName", propertyInformation.DisplayName);
                xmlWriter.WriteAttributeString("value", propertyInformation.Value?.ToString() ?? "null");
                xmlWriter.WriteEndElement();

                propertyInformation.Teardown();
            }

            xmlWriter.WriteEndElement();
        }

        if (recurse)
        {
            foreach (var treeItemChild in treeItem.Children)
            {
                this.ExportItem(treeItemChild, xmlWriter, filter, recurse);
            }
        }

        xmlWriter.WriteEndElement();
    }
}