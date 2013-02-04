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
        private readonly FiltersViewModel filtersViewModel = new FiltersViewModel();
        private readonly SnoopDebugListener snoopDebugListener = new SnoopDebugListener();
        public DebugListenerControl()
        {
            InitializeComponent();
            
            snoopDebugListener.RegisterListener(this);
        }

        private void checkBoxStartListening_Checked(object sender, RoutedEventArgs e)
        {
            Debug.Listeners.Add(snoopDebugListener);
            //PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Verbose;
            PresentationTraceSources.DataBindingSource.Listeners.Add(snoopDebugListener);
        }

        private void checkBoxStartListening_Unchecked(object sender, RoutedEventArgs e)
        {
            Debug.Listeners.Remove(SnoopDebugListener.ListenerName);
            PresentationTraceSources.DataBindingSource.Listeners.Remove(snoopDebugListener);
        }

        public void Write(string str)
        {
            //this.Dispatcher.InvokeActionSafe((
            //this.Dispatcher.InvokeActionSafe(() => DoWrite(str));
            if (!filtersViewModel.IsSet || filtersViewModel.FilterMatches(str))
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Render, () => DoWrite(str));
            }
        }

        private void DoWrite(string str)
        {
            this.textBoxDebugContent.AppendText(str + "\n");            
            this.textBoxDebugContent.ScrollToEnd();
        }


        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            this.textBoxDebugContent.Clear();
        }

        private void buttonClearFilters_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear your filters?", "Clear Filters Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
                filtersViewModel.ClearFilters();
        }

        private void buttonSetFilters_Click(object sender, RoutedEventArgs e)
        {
            SetFiltersWindow setFiltersWindow = new SetFiltersWindow(filtersViewModel);
            setFiltersWindow.Topmost = true;
            setFiltersWindow.Owner = Window.GetWindow(this);
            setFiltersWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            setFiltersWindow.ShowDialog();
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

    public interface IListener
    {
        void Write(string str);

    }

    public class SnoopDebugListener : TraceListener
    {
        private IList<IListener> _listeners = new List<IListener>();

        public void RegisterListener(IListener listener)
        {
            _listeners.Add(listener);
        }

        public const string ListenerName = "SnoopDebugListener";

        public SnoopDebugListener()
        {
            this.Name = ListenerName;
        }

        public override void WriteLine(string str)
        {
            SendDataToListeners(str);
        }

        public override void Write(string str)
        {
            SendDataToListeners(str);
        }

        private void SendDataToListeners(string str)
        {
            foreach (var listener in _listeners)
                listener.Write(str);
        }

        public override void Write(string message, string category)
        {
            SendDataToListeners(message);

            base.Write(message, category);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            SendDataToListeners(message);
            base.TraceEvent(eventCache, source, eventType, id, message);
        }

        public override void Write(object o, string category)
        {
            base.Write(o, category);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            base.TraceData(eventCache, source, eventType, id, data);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            SendDataToListeners(source);
            base.TraceData(eventCache, source, eventType, id, data);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            base.TraceEvent(eventCache, source, eventType, id);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            base.TraceEvent(eventCache, source, eventType, id, format, args);
        }

        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            base.TraceTransfer(eventCache, source, id, message, relatedActivityId);
        }

        public override void WriteLine(object o, string category)
        {
            base.WriteLine(o, category);
        }

        public override void Write(object o)
        {
            base.Write(o);
        }

        public override void WriteLine(object o)
        {
            base.WriteLine(o);
        }

        public override void WriteLine(string message, string category)
        {
            base.WriteLine(message, category);
        }

        public override void Fail(string message)
        {
            base.Fail(message);
        }

        public override void Fail(string message, string detailMessage)
        {
            base.Fail(message, detailMessage);
        }
    }

}
