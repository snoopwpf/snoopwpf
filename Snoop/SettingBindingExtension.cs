// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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
