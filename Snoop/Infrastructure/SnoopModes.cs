// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

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

        public static bool SwallowExceptions { get; set; }

        public static bool IgnoreExceptions { get; set; }
	}
}
