using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Snoop
{
	public class SettingBindingExtension : Binding
	{
		public SettingBindingExtension()
		{

		}

		public SettingBindingExtension(string path) : base(path)
		{

		}

		private void Initialize()
		{
			this.Source = Snoop.Properties.Settings.Default;
			this.Mode = BindingMode.TwoWay;
		}
	}
}
