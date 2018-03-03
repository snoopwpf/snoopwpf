namespace Snoop.Infrastructure
{
    using System.Windows;
    using System.Windows.Controls;

    public class TemplateHelper
    {
        public static object GetChildFromTemplateIfNeeded(FrameworkElement element, string templatePartName)
        {
            if (string.IsNullOrEmpty(templatePartName))
            {
                return element;
            }

            var control = element as Control;
            if (control != null
                && control.Template != null)
            {
                return control.Template.FindName(templatePartName, control);
            }

            return element.FindName(templatePartName);
        }
    }
}