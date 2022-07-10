namespace Snoop.Views.DebugListenerTab;

using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Snoop.Core;
using Snoop.Infrastructure;

public partial class DebugListenerControl : IListener, IDisposable
{
#pragma warning disable CA2213
    private readonly SnoopDebugListener snoopDebugListener = new();
#pragma warning restore CA2213
    private StringBuilder allText = new();
    private bool ignoreWrites;

    public DebugListenerControl()
    {
        this.FiltersViewModel = new FiltersViewModel(Settings.Default.SnoopDebugFilters);
        this.DataContext = this.FiltersViewModel;

        this.InitializeComponent();

        this.snoopDebugListener.RegisterListener(this);

        PresentationTraceSources.SetTraceLevel(this, PresentationTraceLevel.None);
        PresentationTraceSources.SetTraceLevel(this.textBlockStatus, PresentationTraceLevel.None);
        PresentationTraceSources.SetTraceLevel(this.textBoxDebugContent, PresentationTraceLevel.None);
    }

    internal FiltersViewModel FiltersViewModel { get; }

    private void CheckBoxStartListening_Checked(object sender, RoutedEventArgs e)
    {
        Trace.Listeners.Add(this.snoopDebugListener);
        PresentationTraceSources.DataBindingSource.Listeners.Add(this.snoopDebugListener);
    }

    private void CheckBoxStartListening_Unchecked(object sender, RoutedEventArgs e)
    {
        this.CleanupListeners();
    }

    public void Write(string? str)
    {
        this.allText.Append(str);

        if (!this.FiltersViewModel.IsSet || this.FiltersViewModel.FilterMatches(str))
        {
            this.Dispatcher.RunInDispatcherAsync(() => this.DoWrite(str), DispatcherPriority.Send);
        }
    }

    private void DoWrite(string? str)
    {
        // prevent reentrancy
        if (this.ignoreWrites)
        {
            return;
        }

        this.ignoreWrites = true;

        var shouldScrollToEnd = this.textBoxDebugContent.SelectionLength == 0
                                && this.textBoxDebugContent.SelectionStart == this.textBoxDebugContent.Text.Length;
        this.textBoxDebugContent.AppendText(str);

        if (shouldScrollToEnd)
        {
            this.textBoxDebugContent.ScrollToEnd();
            this.textBoxDebugContent.SelectionStart = this.textBoxDebugContent.Text.Length;
        }

        this.ignoreWrites = false;
    }

    private void ButtonClear_Click(object sender, RoutedEventArgs e)
    {
        this.textBoxDebugContent.Clear();
        this.allText = new StringBuilder();
    }

    private void ButtonClearFilters_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to clear your filters?", "Clear Filters Confirmation", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
            this.FiltersViewModel.ClearFilters();
            Settings.Default.SnoopDebugFilters.Clear();
            this.textBoxDebugContent.Text = this.allText.ToString();
        }
    }

    private void ButtonSetFilters_Click(object sender, RoutedEventArgs e)
    {
        var setFiltersWindow = new SetFiltersWindow(this.FiltersViewModel)
        {
            Topmost = true
        };

        setFiltersWindow.ShowDialogEx(this);

        var allLines = this.allText.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        this.textBoxDebugContent.Clear();

        foreach (var line in allLines)
        {
            if (this.FiltersViewModel.FilterMatches(line))
            {
                this.textBoxDebugContent.AppendText(line + Environment.NewLine);
            }
        }
    }

    private void ComboBoxPresentationTraceLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.comboBoxPresentationTraceLevel?.Items is null
            || this.comboBoxPresentationTraceLevel.Items.Count <= this.comboBoxPresentationTraceLevel.SelectedIndex
            || this.comboBoxPresentationTraceLevel.SelectedIndex < 0)
        {
            return;
        }

        var selectedComboBoxItem = this.comboBoxPresentationTraceLevel.Items[this.comboBoxPresentationTraceLevel.SelectedIndex] as ComboBoxItem;
        if (selectedComboBoxItem?.Tag is null)
        {
            return;
        }

        var sourceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), selectedComboBoxItem.Tag.ToString()!);
        PresentationTraceSources.DataBindingSource.Switch.Level = sourceLevel;
    }

    private void CleanupListeners()
    {
        Trace.Listeners.Remove(SnoopDebugListener.ListenerName);
        PresentationTraceSources.DataBindingSource.Listeners.Remove(this.snoopDebugListener);
    }

    public void Dispose()
    {
        this.snoopDebugListener.Dispose();
        this.CleanupListeners();
    }
}