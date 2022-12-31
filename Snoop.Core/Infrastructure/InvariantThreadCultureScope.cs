namespace Snoop.Infrastructure;

using System;
using System.Globalization;
using System.Threading;

public class InvariantThreadCultureScope : IDisposable
{
    private readonly CultureInfo fallbackCulture;

    public InvariantThreadCultureScope()
    {
        this.fallbackCulture = Thread.CurrentThread.CurrentCulture;

        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        Thread.CurrentThread.CurrentCulture = this.fallbackCulture;
    }
}