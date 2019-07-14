namespace Snoop.Infrastructure
{
    using System;
    using System.Windows.Forms;

    public static class ClipboardHelper
    {
        public static void SetText(string text)
        {
            try
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
            }
            catch (Exception exception)
            {
                ErrorDialog.ShowExceptionMessageBox(exception, "Error copying to clipboard", "There was an error copying to the clipboard.\nPlease copy the error details from the above textbox manually.");
            }
        }
    }
}