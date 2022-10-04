namespace Snoop.Core.Tests.Infrastructure.Diagnostics.Providers;

using System;
using System.Collections;
using System.Linq;
using System.Windows.Controls;
using NUnit.Framework;
using Snoop.Infrastructure.Diagnostics.Providers;

[TestFixture]
public class BindingLeakDiagnosticProviderTests
{
    [Test]
    public void Test()
    {
        var provider = new BindingLeakDiagnosticProvider();

        {
            var items = provider.GetGlobalDiagnosticItems();

            Assert.That(items, Is.Empty);
        }

        // Create binding leak
        {
            var control = new ContentControl
            {
                DataContext = new DateTime(2022, 10, 3)
            };

            control.SetBinding(ContentControl.ContentProperty, nameof(DateTime.Year));
        }

        {
            var items = provider.GetGlobalDiagnosticItems()
                .ToList();

            Assert.That(items, Has.Count.EqualTo(1));

            Assert.That(items.First().Description, Is.EqualTo("Property 'Year' from type 'System.DateTime' is bound 1 times causing binding leaks."));
        }
    }
}