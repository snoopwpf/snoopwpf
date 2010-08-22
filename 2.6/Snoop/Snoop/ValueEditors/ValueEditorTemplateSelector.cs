// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	public class ValueEditorTemplateSelector: DataTemplateSelector {
		private DataTemplate standardTemplate;
		private DataTemplate enumTemplate;
		private DataTemplate boolTemplate;
		private DataTemplate brushTemplate;

		public DataTemplate StandardTemplate {
			get { return this.standardTemplate; }
			set { this.standardTemplate = value; }
		}

		public DataTemplate EnumTemplate {
			get { return this.enumTemplate; }
			set { this.enumTemplate = value; }
		}

		public DataTemplate BoolTemplate {
			get { return this.boolTemplate; }
			set { this.boolTemplate = value; }
		}

		public DataTemplate BrushTemplate {
			get { return this.brushTemplate; }
			set { this.brushTemplate = value; }
		}

		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			PropertyInformation property = (PropertyInformation)item;

			if (property.PropertyType.IsEnum)
				return this.EnumTemplate;
			else if (property.PropertyType.Equals(typeof(bool)))
				return this.BoolTemplate;
			else if (typeof(Brush).IsAssignableFrom(property.PropertyType))
				return this.brushTemplate;

			return this.StandardTemplate;
		}
	}
}
