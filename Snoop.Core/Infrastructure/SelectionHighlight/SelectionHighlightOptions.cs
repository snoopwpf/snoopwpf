namespace Snoop.Infrastructure.SelectionHighlight
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;

    public sealed class SelectionHighlightOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public static readonly SelectionHighlightOptions Current = new();

        private Thickness borderThickness = new(4);
        private Brush borderBrush = new SolidColorBrush(new Color
        {
            ScA = .3f,
            ScR = 1
        });

        public Brush BorderBrush
        {
            get => this.borderBrush;
            set
            {
                if (Equals(value, this.borderBrush))
                {
                    return;
                }

                this.borderBrush = value;
                this.OnPropertyChanged();
            }
        }

        public Thickness BorderThickness
        {
            get => this.borderThickness;
            set
            {
                if (value.Equals(this.borderThickness))
                {
                    return;
                }

                this.borderThickness = value;
                this.OnPropertyChanged();
            }
        }

        public bool HighlightSelectedItem { get; set; } = true;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}