namespace Snoop
{

	using System.Collections.ObjectModel;

	public partial class ExpressionsView {

		private ObservableCollection<Expression> expressions = new ObservableCollection<Expression>();

		public ExpressionsView() {
			this.InitializeComponent();
		}

		public ObservableCollection<Expression> Expressions {
			get { return this.expressions; }
		}
	}
}
