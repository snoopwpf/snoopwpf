// Original code for viewing Triggers was taken from https://archive.codeplex.com/?p=wpfinspector which is written by Christian Moser

namespace Snoop.Views.TriggersTab
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Snoop.Views.TriggersTab.Triggers;

    public partial class TriggersView
    {
        private readonly ObservableCollection<TriggerItemBase> triggers = new();

        public TriggersView()
        {
            this.InitializeComponent();

            this.TriggerItems = CollectionViewSource.GetDefaultView(this.triggers);
            this.TriggerItems.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TriggerItemBase.TriggerSource)));

            this.TriggerItems.CollectionChanged += (_, _) => this.HasTriggerItems = this.TriggerItems.IsEmpty;

            this.Loaded += this.HandleLoaded;
            this.Unloaded += this.HandleUnloaded;
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(TriggersView), new PropertyMetadata(default(bool), OnIsSelectedChanged));

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

        public static readonly DependencyProperty RootTargetProperty = DependencyProperty.Register(nameof(RootTarget), typeof(object), typeof(TriggersView), new PropertyMetadata(default, OnRootTargetChanged));

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
            = DependencyProperty.RegisterReadOnly(nameof(TriggerItems), typeof(ICollectionView), typeof(TriggersView), new FrameworkPropertyMetadata(default(ICollectionView)));

        public static readonly DependencyProperty TriggerItemsProperty = TriggerItemsPropertyKey.DependencyProperty;

        public ICollectionView? TriggerItems
        {
            get { return (ICollectionView?)this.GetValue(TriggerItemsProperty); }
            protected set { this.SetValue(TriggerItemsPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey HasTriggerItemsPropertyKey
            = DependencyProperty.RegisterReadOnly(nameof(HasTriggerItems), typeof(bool), typeof(TriggersView), new FrameworkPropertyMetadata(default(bool)));

        public static readonly DependencyProperty HasTriggerItemsProperty = HasTriggerItemsPropertyKey.DependencyProperty;

        public bool HasTriggerItems
        {
            get { return (bool)this.GetValue(HasTriggerItemsProperty); }
            protected set { this.SetValue(HasTriggerItemsPropertyKey, value); }
        }

        public static readonly DependencyProperty SelectedSetterItemProperty = DependencyProperty.Register(nameof(SelectedSetterItem), typeof(SetterItem), typeof(TriggersView), new PropertyMetadata(default(SetterItem)));

        public SetterItem? SelectedSetterItem
        {
            get { return (SetterItem?)this.GetValue(SelectedSetterItemProperty); }
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

        private void UpdateTriggerList(object? target)
        {
            if (target is null)
            {
                return;
            }

            if (target is FrameworkElement fe)
            {
                // If the target does not have an explicit style, try to find the default style
                var style = fe.Style ?? fe.TryFindResource(fe.GetType()) as Style;

                if (style is not null)
                {
                    this.AddTriggers(fe, style, TriggerSource.Style);
                }

                this.AddTriggers(fe, fe.Triggers, TriggerSource.Element);
            }

            if (target is FrameworkContentElement fec)
            {
                // If the target does not have an explicit style, try to find the default style
                var style = fec.Style ?? fec.TryFindResource(fec.GetType()) as Style;

                if (style is not null)
                {
                    this.AddTriggers(fec, style, TriggerSource.Style);
                }
            }

            if (target is Control { Template: { } } control)
            {
                this.AddTriggers(control, control.Template.Triggers, TriggerSource.ControlTemplate);
            }

            switch (target)
            {
                case ContentControl { ContentTemplate: { } } contentControl:
                    this.AddTriggers(contentControl, contentControl.ContentTemplate.Triggers, TriggerSource.DataTemplate);
                    break;

                case ContentPresenter { ContentTemplate: { } } contentPresenter:
                    this.AddTriggers(contentPresenter, contentPresenter.ContentTemplate.Triggers, TriggerSource.DataTemplate);
                    break;
            }
        }

        private void AddTriggers(DependencyObject instance, Style style, TriggerSource source)
        {
            var currentStyle = style;

            while (currentStyle is not null)
            {
                this.AddTriggers(instance, currentStyle.Triggers, source);

                currentStyle = currentStyle.BasedOn;
            }
        }

        private void AddTriggers(DependencyObject instance, IEnumerable<TriggerBase> triggersToAdd, TriggerSource source)
        {
            foreach (var trigger in triggersToAdd)
            {
                var triggerItem = TriggerItemFactory.GetTriggerItem(trigger, instance, source);
                if (triggerItem is not null)
                {
                    this.triggers.Add(triggerItem);
                }
            }
        }
    }
}