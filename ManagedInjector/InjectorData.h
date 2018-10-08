#pragma once

namespace ManagedInjector
{
	public ref class InjectorData : System::Object
	{
	public:

		property System::String^ AssemblyName;
		property System::String^ ClassName;
		property System::String^ MethodName;

		property System::String^ SettingsFile;
	};
}