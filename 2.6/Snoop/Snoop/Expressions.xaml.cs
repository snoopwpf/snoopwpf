// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.ObjectModel;

namespace Snoop
{

	public partial class ExpressionsView
	{
		private ObservableCollection<Expression> expressions = new ObservableCollection<Expression>();

		public ExpressionsView()
		{
			this.InitializeComponent();
		}

		public ObservableCollection<Expression> Expressions
		{
			get { return this.expressions; }
		}
	}
}
