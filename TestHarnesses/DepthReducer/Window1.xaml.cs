using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using System;
using System.Windows.Interop;

namespace DepthReducer
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();

			Populate(_left);
			Populate(_right);
		}

		private void Populate(Border b)
		{
			PropertyInfo[] info = typeof(Brushes).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
			int count = Math.Min(100, info.Length);
			double w = Width / (2 * count);
			double h = Height / (2 * count);
			Thickness padding = new Thickness(w, h, w, h);
			int i;
			for (i = 0; i < count - 1; ++i)
			{
				b.Padding = padding;
				b.Background = (Brush)info[i].GetValue(null, null);
				Border b2 = new Border();
				b.Child = b2;
				b = b2;
			}
			b.Background = (Brush)info[i].GetValue(null, null);
		}
	}
}
