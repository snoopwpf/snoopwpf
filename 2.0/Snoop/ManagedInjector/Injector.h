// Copyright © 2006 Microsoft Corporation.  All Rights Reserved

#pragma once

__declspec( dllexport )
int __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam);

namespace ManagedInjector {

	public ref class Injector: System::Object {

	public:
		static void Launch(System::IntPtr windowHandle, System::Reflection::Assembly^ assemblyName, System::String^ className, System::String^ methodName);
	};
}