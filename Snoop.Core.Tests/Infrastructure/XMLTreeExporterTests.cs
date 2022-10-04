namespace Snoop.Core.Tests.Infrastructure;

using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NUnit.Framework;
using Snoop.Data.Tree;
using Snoop.Infrastructure;
using VerifyNUnit;

[TestFixture]
public class XMLTreeExporterTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Assert correct expected formatting
        Assert.That(default(Point).ToString(), Is.EqualTo("0,0"), CultureInfo.CurrentCulture.NativeName);

        // Required to ensure ScrollViewer attached properties are initialized
        // ReSharper disable once UnusedVariable
        var scrollViewer = new ScrollViewer();
    }

    [Test]
    public Task TestTreeWithoutPropertyFilter()
    {
        var textWriter = new StringWriter();

        var exporter = new XMLTreeExporter();
        exporter.Export(GetTestTreeItem(), textWriter, new(string.Empty, false), true);

        var result = textWriter.ToString();

        return Verifier.Verify(result);
    }

    [Test]
    public Task TestTreeWithPropertyFilter()
    {
        var textWriter = new StringWriter();

        var exporter = new XMLTreeExporter();
        exporter.Export(GetTestTreeItem(), textWriter, new("Height", false), true);

        var result = textWriter.ToString();

        return Verifier.Verify(result);
    }

    [Test]
    public Task TestElementWithoutPropertyFilter()
    {
        var textWriter = new StringWriter();

        var exporter = new XMLTreeExporter();
        exporter.Export(GetTestTreeItem(), textWriter, new(string.Empty, false), false);

        var result = textWriter.ToString();

        return Verifier.Verify(result);
    }

    [Test]
    public Task TestElementWithPropertyFilter()
    {
        var textWriter = new StringWriter();

        var exporter = new XMLTreeExporter();
        exporter.Export(GetTestTreeItem(), textWriter, new("Height", false), false);

        var result = textWriter.ToString();

        return Verifier.Verify(result);
    }

    private static TreeItem GetTestTreeItem()
    {
        var target = new StackPanel();
        target.Children.Add(new TextBlock { Text = "test" });
        target.Children.Add(new Border { Child = new CheckBox { Content = "check" } });

        using var treeService = TreeService.From(TreeType.Visual);
        return treeService.Construct(target, null);
    }
}