// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Snoop
{
	public class ValueEditorTemplateSelector : DataTemplateSelector
	{
        public DataTemplate LongStringTemplate
        {
            get { return this.longStringTemplate; }
            set { this.longStringTemplate = value; }
        }
        private DataTemplate longStringTemplate;

		public DataTemplate StandardTemplate
		{
			get { return this.standardTemplate; }
			set { this.standardTemplate = value; }
		}
		private DataTemplate standardTemplate;

		public DataTemplate EnumTemplate
		{
			get { return this.enumTemplate; }
			set { this.enumTemplate = value; }
		}
		private DataTemplate enumTemplate;

		public DataTemplate BoolTemplate
		{
			get { return this.boolTemplate; }
			set { this.boolTemplate = value; }
		}
		private DataTemplate boolTemplate;

		public DataTemplate BrushTemplate
		{
			get { return this.brushTemplate; }
			set { this.brushTemplate = value; }
		}
		private DataTemplate brushTemplate;

        /// <summary>
        /// Returns the number of lines in the string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private int NumberOfLines(string str)
        {
            var array = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            return array.Length;
        }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			PropertyInformation property = (PropertyInformation)item;

            if (property.Value is string && NumberOfLines((string)property.Value) > 3)
                return this.longStringTemplate;
			else if (property.PropertyType.IsEnum)
				return this.EnumTemplate;
			else if (property.PropertyType.Equals(typeof(bool)))
				return this.BoolTemplate;
			else if ( property.PropertyType.IsGenericType 
				&& Nullable.GetUnderlyingType( property.PropertyType ) == typeof(bool) )
				return this.BoolTemplate;
			else if (typeof(Brush).IsAssignableFrom(property.PropertyType))
				return this.brushTemplate;

			return this.StandardTemplate;
		}
	}
}
