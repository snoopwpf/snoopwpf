using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snoop.DebugListenerTab
{
	public interface IListener
	{
		void Write(string str);
	}
}
