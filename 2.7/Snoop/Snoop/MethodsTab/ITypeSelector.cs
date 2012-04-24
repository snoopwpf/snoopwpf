using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Snoop.MethodsTab
{
    public interface ITypeSelector
    {
        bool? ShowDialog();

        bool? DialogResult { get; }

        object Instance { get; }

        WindowStartupLocation WindowStartupLocation { get; set; }

        string Title { get; set; }
    }
}
