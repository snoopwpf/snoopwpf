// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Views.MethodsTab;

using System.Windows;

public interface ITypeSelector
{
    bool? ShowDialog();

    bool? DialogResult { get; }

    object? Instance { get; }

    WindowStartupLocation WindowStartupLocation { get; set; }

    string Title { get; set; }
}