namespace Snoop.Views.DebugListenerTab;

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;

[Serializable]
public abstract class SnoopFilter : INotifyPropertyChanged
{
    private bool isGrouped;
    private string groupId = string.Empty;
    private bool isDirty;
    private bool isInverse;
    //protected string _isInverseText = string.Empty;

    public void ResetDirtyFlag()
    {
        this.IsDirty = false;
    }

    [XmlIgnore]
    public bool IsDirty
    {
        get
        {
            return this.isDirty;
        }

        protected set
        {
            this.isDirty = value;
            this.RaisePropertyChanged(nameof(this.IsDirty));
        }
    }

    public abstract bool FilterMatches(string? debugLine);

    public virtual bool SupportsGrouping
    {
        get
        {
            return true;
        }
    }

    public bool IsInverse
    {
        get
        {
            return this.isInverse;
        }

        set
        {
            if (value != this.isInverse)
            {
                this.isInverse = value;
                this.RaisePropertyChanged(nameof(this.IsInverse));
                this.RaisePropertyChanged(nameof(this.IsInverseText));
            }
        }
    }

    public string IsInverseText
    {
        get
        {
            return this.isInverse ? "NOT" : string.Empty;
        }
    }

    public bool IsGrouped
    {
        get
        {
            return this.isGrouped;
        }

        set
        {
            this.isGrouped = value;
            this.RaisePropertyChanged(nameof(this.IsGrouped));
            this.groupId = string.Empty;
        }
    }

    public virtual string GroupId
    {
        get
        {
            return this.groupId;
        }

        set
        {
            this.groupId = value;
            this.RaisePropertyChanged(nameof(this.GroupId));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void RaisePropertyChanged(string propertyName)
    {
        this.isDirty = true;
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}