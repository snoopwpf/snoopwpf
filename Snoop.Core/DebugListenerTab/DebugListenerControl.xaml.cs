namespace Snoop.DebugListenerTab
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using Snoop.Infrastructure;

    /// <summary>
    /// Interaction logic for DebugListenerControl.xaml
    /// </summary>
    public partial class DebugListenerControl : UserControl, IListener
    {
        private readonly FiltersViewModel filtersViewModel;// = new FiltersViewModel();
        private readonly SnoopDebugListener snoopDebugListener = new SnoopDebugListener();
        private StringBuilder allText = new StringBuilder();

        public DebugListenerControl()
        {
            filtersViewModel = new FiltersViewModel(Properties.Settings.Default.SnoopDebugFilters);
            this.DataContext = filtersViewModel;

            InitializeComponent();

            snoopDebugListener.RegisterListener(this);
        }

        private void checkBoxStartListening_Checked(object sender, RoutedEventArgs e)
        {
            Trace.Listeners.Add(snoopDebugListener);
            PresentationTraceSources.DataBindingSource.Listeners.Add(snoopDebugListener);
        }

        private void checkBoxStartListening_Unchecked(object sender, RoutedEventArgs e)
        {
            Trace.Listeners.Remove(SnoopDebugListener.ListenerName);
            PresentationTraceSources.DataBindingSource.Listeners.Remove(snoopDebugListener);
        }

        public void Write(string str)
        {
            allText.Append(str + Environment.NewLine);
            if (!filtersViewModel.IsSet || filtersViewModel.FilterMatches(str))
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Render, () => DoWrite(str));
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

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            this.textBoxDebugContent.Clear();
            allText = new StringBuilder();
        }

        private void buttonClearFilters_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear your filters?", "Clear Filters Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                filtersViewModel.ClearFilters();
                Properties.Settings.Default.SnoopDebugFilters = null;
                this.textBoxDebugContent.Text = allText.ToString();
            }
        }

        private void buttonSetFilters_Click(object sender, RoutedEventArgs e)
        {
            SetFiltersWindow setFiltersWindow = new SetFiltersWindow(filtersViewModel);
            setFiltersWindow.Topmost = true;
            setFiltersWindow.ShowDialogEx(this);

            string[] allLines = allText.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            this.textBoxDebugContent.Clear();
            foreach (string line in allLines)
            {
                if (filtersViewModel.FilterMatches(line))
                {
                    this.textBoxDebugContent.AppendText(line + Environment.NewLine);
                }
            }
        }

        private void comboBoxPresentationTraceLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxPresentationTraceLevel == null || this.comboBoxPresentationTraceLevel.Items == null || this.comboBoxPresentationTraceLevel.Items.Count <= this.comboBoxPresentationTraceLevel.SelectedIndex || this.comboBoxPresentationTraceLevel.SelectedIndex < 0)
            {
                return;
            }

            var selectedComboBoxItem = this.comboBoxPresentationTraceLevel.Items[this.comboBoxPresentationTraceLevel.SelectedIndex] as ComboBoxItem;
            if (selectedComboBoxItem == null || selectedComboBoxItem.Tag == null)
            {
                return;
            }


            var sourceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), selectedComboBoxItem.Tag.ToString());
            PresentationTraceSources.DataBindingSource.Switch.Level = sourceLevel;
        }
    }
}
