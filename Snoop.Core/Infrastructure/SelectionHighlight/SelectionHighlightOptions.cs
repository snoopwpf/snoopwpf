namespace Snoop.Infrastructure.SelectionHighlight;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using JetBrains.Annotations;

public sealed class SelectionHighlightOptions : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public static readonly SelectionHighlightOptions Default = new();

    private double borderThickness;

    private Brush borderBrush = null!;

    private bool highlightSelectedItem = true;

    private Pen? pen;
    private Brush? background;

    public SelectionHighlightOptions()
    {
        this.Reset();
    }

    public Brush? Background
    {
        get => this.background;
        set
        {
            if (Equals(value, this.background))
            {
                return;
            }

            this.background = value;
            this.OnPropertyChanged();
        }
    }

    public Brush BorderBrush
    {
        get => this.borderBrush;
        set
        {
            if (Equals(value, this.borderBrush))
            {
                return;
            }

            this.pen = null;
            this.borderBrush = value;
            this.OnPropertyChanged();
        }
    }

    public double BorderThickness
    {
        get => this.borderThickness;
        set
        {
            if (value.Equals(this.borderThickness))
            {
                return;
            }

            this.pen = null;
            this.borderThickness = value;
            this.OnPropertyChanged();
        }
    }

    public bool HighlightSelectedItem
    {
        get => this.highlightSelectedItem;
        set
        {
            if (value == this.highlightSelectedItem)
            {
                return;
            }

            this.highlightSelectedItem = value;
            this.OnPropertyChanged();
        }
    }

    public Pen Pen => this.pen ??= new Pen(this.BorderBrush, this.BorderThickness);

    public void Reset()
    {
        this.Background = null;
        this.BorderThickness = 3D;
        var borderColor = new Color
        {
            ScA = .3f,
            ScR = 1
        };
        this.BorderBrush = new SolidColorBrush(borderColor);
        this.BorderBrush.Freeze();
    }

    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}