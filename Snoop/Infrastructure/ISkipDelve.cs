using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snoop.Infrastructure
{
    public interface ISkipDelve
    {
        object NextValue { get; }

        Type NextValueType { get; }
    }
}
