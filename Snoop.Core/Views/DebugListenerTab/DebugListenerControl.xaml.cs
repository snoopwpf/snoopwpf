namespace Snoop.Views.DebugListenerTab
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using Snoop.Infrastructure;
    using Snoop.Infrastructure.Extensions;

    public partial class DebugListenerControl : IListener
    {
        private readonly FiltersViewModel filtersViewModel; // = new FiltersViewModel();
        private readonly SnoopDebugListener snoopDebugListener = new SnoopDebugListener();
        private StringBuilder allText = new StringBuilder();

        public DebugListenerControl()
        {
            this.filtersViewModel = new FiltersViewModel(Properties.Settings.Default.SnoopDebugFilters);
            this.DataContext = this.filtersViewModel;

            this.InitializeComponent();

            this.snoopDebugListener.RegisterListener(this);
        }

        private void CheckBoxStartListening_Checked(object sender, RoutedEventArgs e)
        {
            Trace.Listeners.Add(this.snoopDebugListener);
            PresentationTraceSources.DataBindingSource.Listeners.Add(this.snoopDebugListener);
        }

        private void CheckBoxStartListening_Unchecked(object sender, RoutedEventArgs e)
        {
            Trace.Listeners.Remove(SnoopDebugListener.ListenerName);
            PresentationTraceSources.DataBindingSource.Listeners.Remove(this.snoopDebugListener);
        }

        public void Write(string str)
        {
            this.allText.Append(str + Environment.NewLine);
            if (!this.filtersViewModel.IsSet || this.filtersViewModel.FilterMatches(str))
            {
                this.Dispatcher.RunInDispatcherAsync(() => this.DoWrite(str), DispatcherPriority.Render);
            }
        }

        private void DoWrite(string str)
        {
            var shouldScrollToEnd = this.textBoxDebugContent.SelectionLength == 0
                                    && this.textBoxDebugContent.SelectionStart == this.textBoxDebugContent.Text.Length;
            this.textBoxDebugContent.AppendText(str + Environment.NewLine);

            if (shouldScrollToEnd)
            {
                this.textBoxDebugContent.ScrollToEnd();
                this.textBoxDebugContent.SelectionStart = this.textBoxDebugContent.Text.Length;
            }
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
                this.filtersViewModel.ClearFilters();
                Properties.Settings.Default.SnoopDebugFilters = null;
                this.textBoxDebugContent.Text = this.allText.ToString();
            }
        }

        private void ButtonSetFilters_Click(object sender, RoutedEventArgs e)
        {
            var setFiltersWindow = new SetFiltersWindow(this.filtersViewModel)
            {
                Topmost = true
            };

            setFiltersWindow.ShowDialogEx(this);

            var allLines = this.allText.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            this.textBoxDebugContent.Clear();

            foreach (var line in allLines)
            {
                if (this.filtersViewModel.FilterMatches(line))
                {
                    this.textBoxDebugContent.AppendText(line + Environment.NewLine);
                }
            }
        }

        private void ComboBoxPresentationTraceLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxPresentationTraceLevel?.Items == null 
                || this.comboBoxPresentationTraceLevel.Items.Count <= this.comboBoxPresentationTraceLevel.SelectedIndex 
                || this.comboBoxPresentationTraceLevel.SelectedIndex < 0)
            {
                return;
            }

            var selectedComboBoxItem = this.comboBoxPresentationTraceLevel.Items[this.comboBoxPresentationTraceLevel.SelectedIndex] as ComboBoxItem;
            if (selectedComboBoxItem?.Tag == null)
            {
                return;
            }

            var sourceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), selectedComboBoxItem.Tag.ToString());
            PresentationTraceSources.DataBindingSource.Switch.Level = sourceLevel;
        }
    }
}
