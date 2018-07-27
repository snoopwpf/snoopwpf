// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#include "stdafx.h"

#include "Injector.h"
#include <vcclr.h>

#using <System.Dll>
#using <System.XML.Dll>

using namespace ManagedInjector;

static unsigned int WM_GOBABYGO = ::RegisterWindowMessage(L"Injector_GOBABYGO!");
static HHOOK _messageHookHandle;

//-----------------------------------------------------------------------------
//Spying Process functions follow
//-----------------------------------------------------------------------------
void Injector::Launch(System::IntPtr windowHandle, InjectorData^ injectorData)
{
	System::String^ transportDataString;

	{
		auto serializer = gcnew System::Xml::Serialization::XmlSerializer(InjectorData::typeid);

	    StringWriter^ stream;
		try
		{
			stream = gcnew System::IO::StringWriter();
		    {
		        serializer->Serialize(stream, injectorData);
	    		transportDataString = stream->ToString();
		    }
		}
		finally
		{
			if (stream != nullptr)
			{
				delete stream;
			}
		}
	}

	pin_ptr<const wchar_t> acmLocal = PtrToStringChars(transportDataString);

	HINSTANCE hinstDLL;	

	if (::GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&MessageHookProc, &hinstDLL))
	{
		LogMessage("GetModuleHandleEx successful", true);
		DWORD processID = 0;
		DWORD threadID = ::GetWindowThreadProcessId((HWND)windowHandle.ToPointer(), &processID);

		if (processID)
		{
			LogMessage("Got process id", true);
			HANDLE hProcess = ::OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
			if (hProcess)
			{
				LogMessage("Got process handle", true);
				int buffLen = (transportDataString->Length + 1) * sizeof(wchar_t);
				void* acmRemote = ::VirtualAllocEx(hProcess, NULL, buffLen, MEM_COMMIT, PAGE_READWRITE);

				if (acmRemote)
				{
					LogMessage("VirtualAllocEx successful", true);
					::WriteProcessMemory(hProcess, acmRemote, acmLocal, buffLen, NULL);
				
					_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);

					if (_messageHookHandle)
					{
						LogMessage("SetWindowsHookEx successful", true);
						::SendMessage((HWND)windowHandle.ToPointer(), WM_GOBABYGO, (WPARAM)acmRemote, 0);
						::UnhookWindowsHookEx(_messageHookHandle);
					}

					::VirtualFreeEx(hProcess, acmRemote, 0, MEM_RELEASE);
				}

				::CloseHandle(hProcess);
			}
		}
		::FreeLibrary(hinstDLL);
	}
}

void Injector::LogMessage(System::String^ message, bool append)
{	            
	System::String ^ applicationDataPath = Environment::GetFolderPath(Environment::SpecialFolder::ApplicationData);
	applicationDataPath += "\\Snoop";

	if (!System::IO::Directory::Exists(applicationDataPath))
	{
		System::IO::Directory::CreateDirectory(applicationDataPath);
	}

	System::String ^ pathname = applicationDataPath + "\\SnoopLog.txt";

	if (!append)    
	{    
		System::IO::File::Delete(pathname);        
	}

	System::IO::FileInfo ^ fi = gcnew System::IO::FileInfo(pathname);
	            
	System::IO::StreamWriter ^ sw = fi->AppendText();   
	sw->WriteLine(System::DateTime::Now.ToString("MM/dd/yyyy HH:mm:ss") + " : " + message);
	sw->Close();
}

__declspec(dllexport) 
LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam)
{
	if (nCode == HC_ACTION)
	{
		CWPSTRUCT* msg = (CWPSTRUCT*)lparam;
		if (msg != NULL && msg->message == WM_GOBABYGO)
		{
			System::Diagnostics::Debug::WriteLine("Got WM_GOBABYGO message");

			wchar_t* acmRemote = (wchar_t*)msg->wParam;

			String^ acmLocal = gcnew System::String(acmRemote);
			System::Diagnostics::Debug::WriteLine(System::String::Format("acmLocal = {0}", acmLocal));

			auto serializer = gcnew System::Xml::Serialization::XmlSerializer(InjectorData::typeid);

			auto stringReader = gcnew System::IO::StringReader(acmLocal);
			auto injectorData = (InjectorData^)serializer->Deserialize(stringReader);

			System::Diagnostics::Debug::WriteLine(String::Format("About to load assembly {0}", injectorData->AssemblyName));
			System::Reflection::Assembly^ assembly = System::Reflection::Assembly::LoadFile(injectorData->AssemblyName);
			if (assembly != nullptr)
			{
				System::Diagnostics::Debug::WriteLine(String::Format("About to load type {0}", injectorData->ClassName));
				System::Type^ type = assembly->GetType(injectorData->ClassName);
				if (type != nullptr)
				{
					System::Diagnostics::Debug::WriteLine(String::Format("Just loaded the type {0}", injectorData->ClassName));

					System::Diagnostics::Debug::WriteLine(String::Format("About to get method info for {0}", injectorData->MethodName));
					System::Reflection::MethodInfo^ methodInfo = type->GetMethod(injectorData->MethodName, System::Reflection::BindingFlags::Static | System::Reflection::BindingFlags::Public);
					if (methodInfo != nullptr)
					{
						System::Diagnostics::Debug::WriteLine(String::Format("Just got method info for {0}", injectorData->MethodName));

						System::Diagnostics::Debug::WriteLine(System::String::Format("About to invoke {0} on type {1}", methodInfo->Name, injectorData->ClassName));
						auto args = gcnew array<System::Object^>(1);
						args[0] = injectorData->SettingsFile;
						Object ^ returnValue = methodInfo->Invoke(nullptr, args);
						if (nullptr == returnValue)
							returnValue = "NULL";
						System::Diagnostics::Debug::WriteLine(String::Format("Return value of {0} on type {1} is {2}", methodInfo->Name, injectorData->ClassName, returnValue));
					}
				}
			}
		}
	}
	return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
}