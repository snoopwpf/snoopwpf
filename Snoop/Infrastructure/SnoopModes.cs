using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snoop.Infrastructure
{
	public static class SnoopModes
	{
		/// <summary>
		/// Whether Snoop is Snooping in a situation where there are multiple dispatchers.
		/// The main Snoop UI is needed for each dispatcher.
		/// </summary>
		public static bool MultipleDispatcherMode { get; set; }
	}
}
