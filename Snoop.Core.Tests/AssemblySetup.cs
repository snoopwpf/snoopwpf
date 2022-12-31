namespace Snoop.Core.Tests;

using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;

[SetUpFixture]
public class AssemblySetup
{
    [OneTimeSetUp]
    public void SetUp()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

        DiffEngine.DiffRunner.Disabled = Debugger.IsAttached == false;
    }
}