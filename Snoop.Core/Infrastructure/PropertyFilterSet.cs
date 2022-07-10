namespace Snoop.Infrastructure;

#pragma warning disable CA1819

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;

[DebuggerDisplay("{" + nameof(DisplayName) + "}")]
[Serializable]
public class PropertyFilterSet : INotifyPropertyChanged
{
    private string? displayName;
    private bool isDefault;
    private bool isEditCommand;
    private bool isReadOnly;
    private string[]? properties;

    public string? DisplayName
    {
        get => this.displayName;
        set
        {
            if (value == this.displayName)
            {
                return;
            }

            this.displayName = value;
            this.OnPropertyChanged();
        }
    }

    public bool IsDefault
    {
        get => this.isDefault;
        set
        {
            if (value == this.isDefault)
            {
                return;
            }

            this.isDefault = value;
            this.OnPropertyChanged();
        }
    }

    public bool IsEditCommand
    {
        get => this.isEditCommand;
        set
        {
            if (value == this.isEditCommand)
            {
                return;
            }

            this.isEditCommand = value;
            this.OnPropertyChanged();
        }
    }

    [IgnoreDataMember]
    public bool IsReadOnly
    {
        get => this.isReadOnly;
        set
        {
            if (value == this.isReadOnly)
            {
                return;
            }

            this.isReadOnly = value;
            this.OnPropertyChanged();
        }
    }

    public string[]? Properties
    {
        get => this.properties;
        set
        {
            if (Equals(value, this.properties))
            {
                return;
            }

            this.properties = value;
            this.OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsPropertyInFilter(PropertyInformation property)
    {
        if (this.Properties is null)
        {
            return false;
        }

        foreach (var filterProp in this.Properties)
        {
            if (property.Name?.Equals(filterProp, StringComparison.OrdinalIgnoreCase) == true
                || property.DisplayName.StartsWith(filterProp, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public PropertyFilterSet Clone()
    {
        var src = this;
        return new PropertyFilterSet
        {
            DisplayName = src.DisplayName,
            IsDefault = src.IsDefault,
            IsEditCommand = src.IsEditCommand,
            IsReadOnly = src.IsReadOnly,
            Properties = (string[]?)src.Properties?.Clone()
        };
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}