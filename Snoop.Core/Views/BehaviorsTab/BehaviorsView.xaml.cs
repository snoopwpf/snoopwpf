namespace Snoop.Views.BehaviorsTab;

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;

public partial class BehaviorsView
{
    public BehaviorsView()
    {
        this.InitializeComponent();

        this.Behaviors = new ObservableCollection<object>();
        this.Behaviors.CollectionChanged += (_, _) => this.HasBehaviors = this.Behaviors.Count != 0;

        this.Loaded += this.HandleLoaded;
        this.Unloaded += this.HandleUnloaded;
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(BehaviorsView), new PropertyMetadata(default(bool), OnIsSelectedChanged));

    public bool IsSelected
    {
        get { return (bool)this.GetValue(IsSelectedProperty); }
        set { this.SetValue(IsSelectedProperty, value); }
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (BehaviorsView)d;
        view.Update();
    }

    public static readonly DependencyProperty RootTargetProperty = DependencyProperty.Register(nameof(RootTarget), typeof(object), typeof(BehaviorsView), new PropertyMetadata(default, OnRootTargetChanged));

    public object RootTarget
    {
        get { return this.GetValue(RootTargetProperty); }
        set { this.SetValue(RootTargetProperty, value); }
    }

    private static void OnRootTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (BehaviorsView)d;
        view.Update();
    }

    private static readonly DependencyProperty BehaviorsProperty = DependencyProperty.Register(nameof(Behaviors), typeof(ObservableCollection<object>), typeof(BehaviorsView), new PropertyMetadata(default(ObservableCollection<object>)));

    public ObservableCollection<object>? Behaviors
    {
        get { return (ObservableCollection<object>?)this.GetValue(BehaviorsProperty); }
        set { this.SetValue(BehaviorsProperty, value); }
    }

    private static readonly DependencyProperty SelectedBehaviorProperty = DependencyProperty.Register(nameof(SelectedBehavior), typeof(object), typeof(BehaviorsView), new PropertyMetadata(default(object)));

    public object? SelectedBehavior
    {
        get { return this.GetValue(SelectedBehaviorProperty); }
        set { this.SetValue(SelectedBehaviorProperty, value); }
    }

    private static readonly DependencyPropertyKey HasBehaviorsPropertyKey = DependencyProperty.RegisterReadOnly(nameof(HasBehaviors), typeof(bool), typeof(BehaviorsView), new FrameworkPropertyMetadata(default(bool)));

    public static readonly DependencyProperty HasBehaviorsProperty = HasBehaviorsPropertyKey.DependencyProperty;

    public bool HasBehaviors
    {
        get { return (bool)this.GetValue(HasBehaviorsProperty); }
        protected set { this.SetValue(HasBehaviorsPropertyKey, value); }
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
        this.Behaviors?.Clear();
    }

    private void Update()
    {
        // Always cleanup first
        this.Cleanup();

        if (this.IsSelected)
        {
            this.UpdateBehaviorList(this.RootTarget);
        }
    }

    private void UpdateBehaviorList(object target)
    {
        if (target is DependencyObject depObj)
        {
            this.AddBehaviorsFromType(depObj, "System.Windows.Interactivity.Interaction, System.Windows.Interactivity");
            this.AddBehaviorsFromType(depObj, "Microsoft.Xaml.Behaviors.Interaction, Microsoft.Xaml.Behaviors");
        }
    }

    private void AddBehaviorsFromType(DependencyObject dependencyObject, string assemblyQualifiedName)
    {
        var interactivityType = Type.GetType(assemblyQualifiedName, false);

        if (interactivityType is null)
        {
            return;
        }

        var getBehaviorsMethod = interactivityType.GetMethod("GetBehaviors", BindingFlags.Static | BindingFlags.Public);

        if (getBehaviorsMethod is null)
        {
            return;
        }

        var behaviorsToAdd = getBehaviorsMethod.Invoke(null, new object[] { dependencyObject }) as IEnumerable;

        if (behaviorsToAdd is null)
        {
            return;
        }

        foreach (var behavior in behaviorsToAdd)
        {
            if (behavior is null)
            {
                continue;
            }

            this.Behaviors?.Add(behavior);
        }
    }
}