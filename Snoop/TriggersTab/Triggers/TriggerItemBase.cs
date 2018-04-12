namespace Snoop.TriggersTab.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;

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
        private readonly List<ConditionItem> conditions = new List<ConditionItem>();
        private readonly List<SetterItem> setters = new List<SetterItem>();
        private bool isActive;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TriggerItemBase" /> class.
        /// </summary>
        protected TriggerItemBase(DependencyObject instance, TriggerType triggerType, TriggerSource triggerSource)
        {
            this.Instance = instance;
            this.TriggerType = triggerType;
            this.TriggerSource = triggerSource;
        }

        /// <summary>
        ///     Gets or the setters.
        /// </summary>
        public ICollectionView Setters { get; protected set; }

        /// <summary>
        ///     Gets the conditions.
        /// </summary>
        public ICollectionView Conditions { get; protected set; }

        /// <summary>
        ///     Gets the source of the trigger.
        /// </summary>
        public TriggerSource TriggerSource { get; private set; }

        /// <summary>
        ///     Gets the type of the trigger.
        /// </summary>
        public TriggerType TriggerType { get; private set; }

        /// <summary>
        ///     Gets if the trigger is currently active
        /// </summary>
        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                this.isActive = value;
                var handler = this.PropertyChanged;
                if (handler != null)
                {
                    handler.Invoke(this, new PropertyChangedEventArgs("IsActive"));
                }
            }
        }

        protected DependencyObject Instance { get; private set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        ///     Initializes this trigger.
        /// </summary>
        public void Initialize()
        {
            this.setters.AddRange(this.GetSetters());
            this.Setters = new ListCollectionView(this.setters);

            this.conditions.AddRange(this.GetConditions());

            foreach (var condition in this.conditions)
            {
                condition.StateChanged += this.OnConditionStateChanged;
            }

            this.Conditions = new ListCollectionView(this.conditions);

            this.OnConditionStateChanged(this, EventArgs.Empty);
        }

        protected abstract IEnumerable<SetterItem> GetSetters();

        protected abstract IEnumerable<ConditionItem> GetConditions();

        #region Private Members

        private void OnConditionStateChanged(object sender, EventArgs e)
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
}