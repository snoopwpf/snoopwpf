namespace SimpleUIAutomation
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;

    public class AutomationDriver
    {
        public static int StartAutomation(string settingsFile)
        {
            if (Application.Current == null)
            {
                return 1;
            }

            Application.Current.Dispatcher.Invoke(async () => await RunScript(settingsFile));

            return 0;
        }

        private static async Task RunScript(string settingsFile)
        {
            var scriptLines = File.ReadAllLines(settingsFile, Encoding.UTF8);

            foreach (var scriptLine in scriptLines)
            {
                var splittedLine = scriptLine.Split(' ');

                if (scriptLine.StartsWith("Click"))
                {
                    var uid = splittedLine[1];
                    var button = FindByUid<ButtonBase>(Application.Current.MainWindow, uid);
                    Click(button);
                }
                else if (scriptLine.StartsWith("Input"))
                {
                    var uid = splittedLine[1];
                    var button = FindByUid<TextBox>(Application.Current.MainWindow, uid);
                    Input(button, splittedLine[2]);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        private static void Click(ButtonBase button)
        {
            var clickMethod = button.GetType().GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            clickMethod.Invoke(button, null);
        }

        private static void Input(TextBox textBox, string text)
        {
            textBox.Text = text;
        }

        private static TControlType FindByUid<TControlType>(DependencyObject parent, string uid)
            where TControlType : DependencyObject
        {
            return Find<TControlType>(parent, o => AutomationProperties.GetAutomationId(o) == uid || (string)o.GetValue(UIElement.UidProperty) == uid);
        }

        private static TControlType Find<TControlType>(DependencyObject parent, Predicate<TControlType> predicate)
            where TControlType : DependencyObject
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // todo: filter for visibility
                if (child is TControlType castedChild
                    && predicate(castedChild))
                {
                    return castedChild;
                }

                var innerResult = Find(child, predicate);

                if (innerResult != null)
                {
                    return innerResult;
                }
            }

            return null;
        }
    }
}