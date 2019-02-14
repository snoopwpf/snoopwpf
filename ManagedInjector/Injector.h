// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma once

__declspec(dllexport)
LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam);

using namespace System;
using namespace System::IO;

namespace ManagedInjector
{
	ref class InjectorData;

	public ref class Injector : System::Object
	{
		public:

		static void Launch(System::IntPtr windowHandle, InjectorData^ transportData);

		static void LogMessage(System::String^ message, bool append);
	};
}