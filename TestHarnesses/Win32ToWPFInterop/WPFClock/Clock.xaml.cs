namespace WpfClockNS
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;

    public partial class Clock : ContentControl
    {
        private DispatcherTimer? dayTimer;

        public Clock()
        {
            this.InitializeComponent();
            this.Loaded += new RoutedEventHandler(this.Clock_Loaded);
        }

        private void Clock_Loaded(object sender, RoutedEventArgs e)
        {
            // set the datacontext to be today's date
            DateTime now = DateTime.Now;
            this.DataContext = now.Day.ToString();

            // then set up a timer to fire at the start of tomorrow, so that we can update
            // the datacontext
            this.dayTimer = new DispatcherTimer();
            this.dayTimer.Interval = new TimeSpan(1, 0, 0, 0) - now.TimeOfDay;
            this.dayTimer.Tick += new EventHandler(this.OnDayChange);
            this.dayTimer.Start();

            // finally, seek the timeline, which assumes a beginning at midnight, to the appropriate
            // offset
            Storyboard sb = (Storyboard)this.PodClock.FindResource("sb");
            sb.Begin(this.PodClock, HandoffBehavior.SnapshotAndReplace, true);
            sb.Seek(this.PodClock, now.TimeOfDay, TimeSeekOrigin.BeginTime);
        }

        private void OnDayChange(object sender, EventArgs e)
        {
            // date has changed, update the datacontext to reflect today's date
            DateTime now = DateTime.Now;
            this.DataContext = now.Day.ToString();
            this.dayTimer!.Interval = new TimeSpan(1, 0, 0, 0);
        }
    }
}
