// Original code for viewing Triggers was taken from https://archive.codeplex.com/?p=wpfinspector which is written by Christian Moser

namespace Snoop.TriggersTab
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Snoop.TriggersTab.Triggers;

    public partial class TriggersView
    {
        private readonly ObservableCollection<TriggerItemBase> triggers = new ObservableCollection<TriggerItemBase>();

        public TriggersView()
        {
            this.InitializeComponent();

            this.TriggerItems = CollectionViewSource.GetDefaultView(this.triggers);
            this.TriggerItems.GroupDescriptions.Add(new PropertyGroupDescription("TriggerSource"));

            this.TriggerItems.CollectionChanged += (s, e) => this.HasTriggerItems = this.TriggerItems.IsEmpty;

            this.Loaded += this.HandleLoaded;
            this.Unloaded += this.HandleUnloaded;
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(TriggersView), new PropertyMetadata(default(bool), OnIsSelectedChanged));

        public bool IsSelected
        {
            get { return (bool)this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (TriggersView)d;

            view.Update();
        }

        public static readonly DependencyProperty RootTargetProperty = DependencyProperty.Register("RootTarget", typeof(object), typeof(TriggersView), new PropertyMetadata(default(object), OnRootTargetChanged));        

        public object RootTarget
        {
            get { return (object)this.GetValue(RootTargetProperty); }
            set { this.SetValue(RootTargetProperty, value); }
        }

        private static void OnRootTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (TriggersView)d;

            view.Update();
        }

        private static readonly DependencyPropertyKey TriggerItemsPropertyKey
            = DependencyProperty.RegisterReadOnly("TriggerItems", typeof(ICollectionView), typeof(TriggersView), new FrameworkPropertyMetadata(default(ICollectionView)));

        public static readonly DependencyProperty TriggerItemsProperty = TriggerItemsPropertyKey.DependencyProperty;

        public ICollectionView TriggerItems
        {
            get { return (ICollectionView)this.GetValue(TriggerItemsProperty); }
            protected set { this.SetValue(TriggerItemsPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey HasTriggerItemsPropertyKey
            = DependencyProperty.RegisterReadOnly("HasTriggerItems", typeof(bool), typeof(TriggersView), new FrameworkPropertyMetadata(default(bool)));

        public static readonly DependencyProperty HasTriggerItemsProperty = HasTriggerItemsPropertyKey.DependencyProperty;

        public bool HasTriggerItems
        {
            get { return (bool)this.GetValue(HasTriggerItemsProperty); }
            protected set { this.SetValue(HasTriggerItemsPropertyKey, value); }
        }

        public static readonly DependencyProperty SelectedSetterItemProperty = DependencyProperty.Register("SelectedSetterItem", typeof(SetterItem), typeof(TriggersView), new PropertyMetadata(default(SetterItem)));

        public SetterItem SelectedSetterItem
        {
            get { return (SetterItem)this.GetValue(SelectedSetterItemProperty); }
            set { this.SetValue(SelectedSetterItemProperty, value); }
        }

        private void HandleLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.Update();
        }

        private void HandleUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.Cleanup();
        }

        private void Cleanup()
        {
            foreach (var triggerItem in this.triggers)
            {
                triggerItem.Dispose();
            }

            this.triggers.Clear();
        }

        private void Update()
        {            
            // Always cleanup first
            this.Cleanup();

            if (this.IsSelected)
            {
                this.UpdateTriggerList(this.RootTarget);
            }
        }

        private void UpdateTriggerList(object target)
        {
            if (target == null)
            {
                return;
            }

            var fe = target as FrameworkElement;
            if (fe != null)
            {
                if (fe.Style != null)
                {
                    this.AddTriggers(fe, fe.Style.Triggers, TriggerSource.Style);
                }

                this.AddTriggers(fe, fe.Triggers, TriggerSource.Element);
            }

            var control = target as Control;
            if (control != null && control.Template != null)
            {
                this.AddTriggers(control, control.Template.Triggers, TriggerSource.ControlTemplate);
            }

            var contentControl = target as ContentControl;
            if (contentControl != null && contentControl.ContentTemplate != null)
            {
                this.AddTriggers(contentControl, contentControl.ContentTemplate.Triggers, TriggerSource.DataTemplate);
            }

            var contentPresenter = target as ContentPresenter;
            if (contentPresenter != null && contentPresenter.ContentTemplate != null)
            {
                this.AddTriggers(contentPresenter, contentPresenter.ContentTemplate.Triggers, TriggerSource.DataTemplate);
            }
        }

        private void AddTriggers(FrameworkElement instance, IEnumerable<TriggerBase> triggersToAdd, TriggerSource source)
        {
            foreach (var trigger in triggersToAdd)
            {
                var triggerItem = TriggerItemFactory.GetTriggerItem(trigger, instance, source);
                if (triggerItem != null)
                {
                    this.triggers.Add(triggerItem);
                }
            }
        }       
    }
}