using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Windows.Controls;

namespace Snoop.Shell
{
    public class CommandCompletion
    {
        private Runspace runspace;
        private TextBox commandTextBox;
        private TextBox outputTextBox;

        int _currentCompletion;
        string _initialText;
        string _resultText;
        int _resultCaretIndex = -1;
        int _completionIndex = -1;

        enum Completion
        {
            TabExpansion2,
            TabExpansion,
            None
        }

        Completion? _completionType;
        

        public CommandCompletion(Runspace runspace, TextBox commandTextBox, TextBox outputTextBox)
        {
            this.runspace = runspace;
            this.commandTextBox = commandTextBox;
            this.outputTextBox = outputTextBox;
        }

        private void CheckTabExpansion()
        {
            try
            {
                // Should find TabExpansion2 in PS3+, TabExpansion in PS2. But test functionality rather than version.1
                using (var pipeline2 = this.runspace.CreatePipeline("$Function:TabExpansion2 -ne $null", false))
                {
                    var result2 = pipeline2.Invoke().First();
                    if ((bool)result2.BaseObject)
                    {
                        _completionType = Completion.TabExpansion2;
                        return;
                    }
                }
                using (var pipeline1 = this.runspace.CreatePipeline("$Function:TabExpansion -ne $null", false))
                {
                    var result1 = pipeline1.Invoke().First();
                    if ((bool)result1.BaseObject)
                    {
                        _completionType = Completion.TabExpansion;
                        return;
                    }
                }
            }
            catch (Exception) { }
            _completionType = Completion.None;
        }
        private Completion GetCompletionType()
        {
            if (!_completionType.HasValue)
            {
                CheckTabExpansion();
            }
            return _completionType.Value;
        }

        public void CompleteCommand(bool reverse)
        {
            if (commandTextBox.Text.Length > 0)
            {
                try
                {
                    DoCompletion(reverse, GetCompletionType());
                }
                catch (Exception ex)
                {
                    this.outputTextBox.AppendText(string.Format("Oops! Uncaught exception attempting tab completion on the PowerShell runspace: {0}\n", ex.Message));
                }
            }

            this.outputTextBox.ScrollToEnd();
        }

        /// <summary>
        /// Do completion by TabCompletion or TabCompletion2 functions. TabCompletion will work based on final
        /// token of the input, while TabCompletion2 takes into account cursor position.
        /// </summary>
        private void DoCompletion(bool reverse, Completion completionType)
        {
            if (completionType == Completion.None) return;

            // if unchanged text & position, continue through complete buffer
            if (commandTextBox.Text == _resultText &&
                (completionType == Completion.TabExpansion || commandTextBox.CaretIndex == _resultCaretIndex))
            {
                _currentCompletion += (reverse ? -1 : 1);
            }
            else
            {
                _initialText = commandTextBox.Text;
                _currentCompletion = reverse ? -1 : 0;
                _completionIndex = commandTextBox.CaretIndex;
            }

            using (var pipe = runspace.CreatePipeline())
            {
                string completion;
                int replaceIndex, replaceLength;
                if (completionType == Completion.TabExpansion2)
                {
                    var cmd = new Command("TabExpansion2");
                    cmd.Parameters.Add("InputScript", _initialText);
                    cmd.Parameters.Add("CursorColumn", _completionIndex);
                    pipe.Commands.Add(cmd);

                    var result = pipe.Invoke().First();
                    var completions = result.Properties["CompletionMatches"].Value as IList;

                    if (completions.Count == 0) return;

                    if (_currentCompletion < 0) { _currentCompletion += completions.Count; }
                    _currentCompletion = _currentCompletion % completions.Count;

                    completion = (string)PSObject.AsPSObject(completions[_currentCompletion]).Properties["CompletionText"].Value;
                    replaceIndex = (int)result.Properties["ReplacementIndex"].Value;
                    replaceLength = (int)result.Properties["ReplacementLength"].Value;
                }
                else
                {
                    var token = _initialText.Split(null).LastOrDefault();
                    if (string.IsNullOrEmpty(token))
                        return;

                    var cmd = new Command("TabExpansion");
                    cmd.Parameters.Add("line", _initialText);
                    cmd.Parameters.Add("lastWord", token);
                    pipe.Commands.Add(cmd);

                    var completions = pipe.Invoke()
                        .Select(psobj => (string)psobj.BaseObject)
                        .ToList();

                    if (completions.Count == 0) return;

                    if (_currentCompletion < 0) { _currentCompletion += completions.Count; }
                    _currentCompletion = _currentCompletion % completions.Count;

                    completion = completions[_currentCompletion];
                    // Always replacing the last token in the string
                    replaceIndex = _initialText.LastIndexOf(token);
                    replaceLength = token.Length;
                }

                if (!string.IsNullOrEmpty(completion))
                {
                    // Don't replace in the current text, the replacement length will be wrong
                    commandTextBox.Text = _initialText.Remove(replaceIndex, replaceLength).Insert(replaceIndex, completion);
                    commandTextBox.CaretIndex = replaceIndex + completion.Length;

                    _resultCaretIndex = commandTextBox.CaretIndex;
                    _resultText = commandTextBox.Text;
                }
            }
        }
    }
}
