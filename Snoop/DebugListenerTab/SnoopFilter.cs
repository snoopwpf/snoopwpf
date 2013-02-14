using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Snoop.DebugListenerTab
{
    [Serializable]
    public abstract class SnoopFilter : INotifyPropertyChanged
    {
        protected bool _isGrouped = false;
        protected string _groupId = string.Empty;
        protected bool _isDirty = false;

        public void ResetDirtyFlag()
        {
            _isDirty = false;
        }

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }
        }

        public abstract bool FilterMatches(string debugLine);

        public virtual bool SupportsGrouping
        {
            get
            {
                return true;
            }
        }

        public bool IsGrouped
        {
            get
            {
                return _isGrouped;
            }
            set
            {
                _isGrouped = value;
                this.RaisePropertyChanged("IsGrouped");
                GroupId = string.Empty;
            }
        }

        public virtual string GroupId
        {
            get
            {
                return _groupId;
            }
            set
            {
                _groupId = value;
                this.RaisePropertyChanged("GroupId");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            _isDirty = true;
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
