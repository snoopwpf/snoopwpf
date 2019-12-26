namespace Snoop.DebugListenerTab
{
    using System;
    using System.Text.RegularExpressions;

    [Serializable]
    public class SnoopSingleFilter : SnoopFilter, ICloneable
    {
        private string text;

        public SnoopSingleFilter()
        {
            this.Text = string.Empty;
        }

        public FilterType FilterType { get; set; }

        public string Text
        {
            get
            {
                return this.text;
            }

            set
            {
                this.text = value;
                this.RaisePropertyChanged(nameof(this.Text));
            }
        }

        public override bool FilterMatches(string debugLine)
        {
            debugLine = debugLine.ToLower();
            var text = this.Text.ToLower();
            var filterMatches = false;
            switch (this.FilterType)
            {
                case FilterType.Contains:
                    filterMatches = debugLine.Contains(text);
                    break;
                case FilterType.StartsWith:
                    filterMatches = debugLine.StartsWith(text);
                    break;
                case FilterType.EndsWith:
                    filterMatches = debugLine.EndsWith(text);
                    break;
                case FilterType.RegularExpression:
                    filterMatches = TryMatch(debugLine, text);
                    break;
            }

            if (this.IsInverse)
            {
                filterMatches = !filterMatches;
            }

            return filterMatches;
        }

        private static bool TryMatch(string input, string pattern)
        {
            try
            {
                return Regex.IsMatch(input, pattern);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object Clone()
        {
            var newFilter = new SnoopSingleFilter
            {
                IsGrouped = this.IsGrouped,
                GroupId = this.GroupId,
                text = this.text,
                FilterType = this.FilterType,
                IsInverse = this.IsInverse
            };
            return newFilter;
        }
    }
}
