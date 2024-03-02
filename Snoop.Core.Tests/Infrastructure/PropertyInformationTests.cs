namespace Snoop.Core.Tests.Infrastructure;

using System;
using System.Linq;
using NUnit.Framework;
using Snoop.Infrastructure;

[TestFixture]
public class PropertyInformationTests
{
    [Test]
    public void TestGetAllProperties()
    {
        {
            var properties = PropertyInformation.GetAllProperties(new RegularClass(), Array.Empty<Attribute>());
            Assert.That(properties.Select(x => x.Name).ToArray(), Is.EqualTo(new[] { "MyProperty" }));
        }

        {
            var properties = PropertyInformation.GetAllProperties(new RegularInheritedClasWithNewProperty(), Array.Empty<Attribute>());
            Assert.That(properties.Select(x => x.Name).ToArray(), Is.EqualTo(new[] { "MyProperty", "SecondProperty" }));
        }
    }

    private class RegularClass
    {
        public object? MyProperty { get; set; }
    }

    private class RegularInheritedClasWithNewProperty : RegularClass
    {
        public new object? MyProperty { get; set; }

        public object? SecondProperty { get; set; }
    }
}