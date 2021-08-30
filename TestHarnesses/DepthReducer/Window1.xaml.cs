namespace DepthReducer
{
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public partial class Window1
    {
        public Window1()
        {
            this.InitializeComponent();

            this.Populate(this.left);
            this.Populate(this.right);
        }

        private void Populate(Border b)
        {
            var brushes = typeof(Brushes).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
            var info = brushes.Concat(brushes).ToArray();
            var count = info.Length;
            var w = this.Width / (2 * count);
            var h = this.Height / (2 * count);
            var padding = new Thickness(w, h, w, h);
            int i;
            for (i = 0; i < count - 1; ++i)
            {
                b.Padding = padding;
                b.Background = (Brush)info[i].GetValue(null, null);
                Border b2 = new();
                b.Child = b2;
                b = b2;
            }

            b.Background = (Brush)info[i].GetValue(null, null);
        }
    }
}