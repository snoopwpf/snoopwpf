namespace Snoop.Core.Tests.Infrastructure;

using System;
using NUnit.Framework;
using Snoop.Infrastructure.Helpers;

[TestFixture]
public class UtilsTests
{
    [Test]
    public void TestIgnoreErrors()
    {
        Assert.That(ErrorAction, Throws.Exception);
        Assert.That(Utils.IgnoreErrors(ErrorAction), Is.False);
        Assert.That(Utils.IgnoreErrors(ErrorAction, true), Is.True);
    }

    private static bool ErrorAction()
    {
        throw new Exception();
    }
}