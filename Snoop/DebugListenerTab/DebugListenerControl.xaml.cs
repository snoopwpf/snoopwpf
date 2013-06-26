using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Snoop.Infrastructure;
using System.Windows.Threading;

namespace Snoop.DebugListenerTab
{
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
			Debug.Listeners.Add(snoopDebugListener);
			PresentationTraceSources.DataBindingSource.Listeners.Add(snoopDebugListener);
		}

		private void checkBoxStartListening_Unchecked(object sender, RoutedEventArgs e)
		{
			Debug.Listeners.Remove(SnoopDebugListener.ListenerName);
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
			this.textBoxDebugContent.AppendText(str + Environment.NewLine);
			this.textBoxDebugContent.ScrollToEnd();
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
			setFiltersWindow.Owner = Window.GetWindow(this);
			setFiltersWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			setFiltersWindow.ShowDialog();

            string[] allLines = allText.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            this.textBoxDebugContent.Clear();
            foreach (string line in allLines)
            {
                if (filtersViewModel.FilterMatches(line))
                    this.textBoxDebugContent.AppendText(line + Environment.NewLine);
            }
		}

		private void comboBoxPresentationTraceLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.comboBoxPresentationTraceLevel == null || this.comboBoxPresentationTraceLevel.Items == null || this.comboBoxPresentationTraceLevel.Items.Count <= this.comboBoxPresentationTraceLevel.SelectedIndex || this.comboBoxPresentationTraceLevel.SelectedIndex < 0)
				return;

			var selectedComboBoxItem = this.comboBoxPresentationTraceLevel.Items[this.comboBoxPresentationTraceLevel.SelectedIndex] as ComboBoxItem;
			if (selectedComboBoxItem == null || selectedComboBoxItem.Tag == null)
				return;


			var sourceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), selectedComboBoxItem.Tag.ToString());
			PresentationTraceSources.DataBindingSource.Switch.Level = sourceLevel;
		}
	}
}
