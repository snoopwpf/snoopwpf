namespace Snoop.Views.TriggersTab.Triggers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using JetBrains.Annotations;

public enum TriggerType
{
    Trigger,
    DataTrigger,
    EventTrigger,
    MultiTrigger,
    MultiDataTrigger
}

public enum TriggerSource
{
    Style,
    ControlTemplate,
    Element,
    DataTemplate
}

public abstract class TriggerItemBase : INotifyPropertyChanged, IDisposable
{
    private readonly TriggerBase trigger;

    private readonly List<ConditionItem> conditions = new();
    private readonly List<SetterItem> setters = new();
    private readonly List<TriggerActionItem> enterActions = new();
    private readonly List<TriggerActionItem> exitActions = new();
    private bool isActive;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TriggerItemBase" /> class.
    /// </summary>
    protected TriggerItemBase(TriggerBase trigger, DependencyObject instance, TriggerType triggerType, TriggerSource triggerSource)
    {
        this.trigger = trigger;
        this.Instance = instance;
        this.TriggerType = triggerType;
        this.TriggerSource = triggerSource;
    }

    /// <summary>
    ///     Gets the conditions.
    /// </summary>
    public ICollectionView? Conditions { get; private set; }

    /// <summary>
    ///     Gets or the setters.
    /// </summary>
    public ICollectionView? Setters { get; private set; }

    /// <summary>
    ///     Gets or the EnterActions.
    /// </summary>
    public ICollectionView? EnterActions { get; private set; }

    /// <summary>
    ///     Gets or the ExitActions.
    /// </summary>
    public ICollectionView? ExitActions { get; private set; }

    /// <summary>
    ///     Gets the source of the trigger.
    /// </summary>
    public TriggerSource TriggerSource { get; }

    /// <summary>
    ///     Gets the type of the trigger.
    /// </summary>
    public TriggerType TriggerType { get; }

    /// <summary>
    ///     Gets if the trigger is currently active
    /// </summary>
    public bool IsActive
    {
        get { return this.isActive; }

        set
        {
            this.isActive = value;
            this.RaisePropertyChanged(nameof(this.IsActive));
        }
    }

    protected DependencyObject Instance { get; }

    #region IDisposable Members

    public virtual void Dispose()
    {
        foreach (var condition in this.conditions)
        {
            condition.StateChanged -= this.OnConditionStateChanged;
            condition.Dispose();
        }

        foreach (var setter in this.setters)
        {
            setter.Dispose();
        }
    }

    #endregion

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void RaisePropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    /// <summary>
    ///     Initializes this trigger.
    /// </summary>
    public void Initialize()
    {
        this.conditions.AddRange(this.GetConditions());

        foreach (var condition in this.conditions)
        {
            condition.StateChanged += this.OnConditionStateChanged;
        }

        this.Conditions = new ListCollectionView(this.conditions);

        this.setters.AddRange(this.GetSetters());
        this.Setters = new ListCollectionView(this.setters);

        this.enterActions.AddRange(this.GetEnterActions());
        this.EnterActions = new ListCollectionView(this.enterActions);

        this.exitActions.AddRange(this.GetExitActions());
        this.ExitActions = new ListCollectionView(this.exitActions);

        this.OnConditionStateChanged(this, EventArgs.Empty);
    }

    protected abstract IEnumerable<ConditionItem> GetConditions();

    protected abstract IEnumerable<SetterItem> GetSetters();

    protected virtual IEnumerable<TriggerActionItem> GetEnterActions()
    {
        return this.trigger.EnterActions.Select(x => TriggerActionItemFactory.GetTriggerActionItem(x, this.Instance, this.TriggerSource));
    }

    protected virtual IEnumerable<TriggerActionItem> GetExitActions()
    {
        return this.trigger.ExitActions.Select(x => TriggerActionItemFactory.GetTriggerActionItem(x, this.Instance, this.TriggerSource));
    }

    #region Private Members

    private void OnConditionStateChanged(object? sender, EventArgs e)
    {
        if (this.conditions.Any(condition => !condition.IsActive))
        {
            this.IsActive = false;
            return;
        }

        this.IsActive = true;
    }

    #endregion
}