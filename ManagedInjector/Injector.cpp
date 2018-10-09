// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#include "stdafx.h"

#include "Injector.h"
#include "InjectorData.h"
#include <vcclr.h>

#using <System.Dll>
#using <System.XML.Dll>

using namespace ManagedInjector;

static unsigned int WM_GOBABYGO = ::RegisterWindowMessage(L"Injector_GOBABYGO!");
static HHOOK _messageHookHandle;

//-----------------------------------------------------------------------------
//Spying Process functions follow
//-----------------------------------------------------------------------------
void Injector::Launch(IntPtr windowHandle, InjectorData^ injectorData)
{
	auto transportDataString = String::Empty;

	{
		auto serializer = gcnew Xml::Serialization::XmlSerializer(InjectorData::typeid);

	    StringWriter^ stream = nullptr;
		try
		{
			stream = gcnew StringWriter();
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

	const pin_ptr<const wchar_t> acmLocal = PtrToStringChars(transportDataString);

	HINSTANCE hinstDLL;	

	if (::GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&MessageHookProc, &hinstDLL))
	{
		LogMessage("GetModuleHandleEx successful", true);
		DWORD processID = 0;
		DWORD threadID = GetWindowThreadProcessId((HWND)windowHandle.ToPointer(), &processID);

		if (processID)
		{
			LogMessage("Got process id", true);
			HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
			if (hProcess)
			{
				LogMessage("Got process handle", true);
				int buffLen = (transportDataString->Length + 1) * sizeof(wchar_t);
				void* acmRemote = VirtualAllocEx(hProcess, nullptr, buffLen, MEM_COMMIT, PAGE_READWRITE);

				if (acmRemote)
				{
					LogMessage("VirtualAllocEx successful", true);
					WriteProcessMemory(hProcess, acmRemote, acmLocal, buffLen, nullptr);
				
					_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);

					if (_messageHookHandle)
					{
						LogMessage("SetWindowsHookEx successful", true);
						SendMessage((HWND)windowHandle.ToPointer(), WM_GOBABYGO, (WPARAM)acmRemote, 0);
						UnhookWindowsHookEx(_messageHookHandle);
					}

					VirtualFreeEx(hProcess, acmRemote, 0, MEM_RELEASE);
				}

				CloseHandle(hProcess);
			}
		}
		FreeLibrary(hinstDLL);
	}
}

void Injector::LogMessage(String^ message, bool append)
{	            
	String ^ applicationDataPath = Environment::GetFolderPath(Environment::SpecialFolder::ApplicationData);
	applicationDataPath += "\\Snoop";

	if (!Directory::Exists(applicationDataPath))
	{
		Directory::CreateDirectory(applicationDataPath);
	}

	String ^ pathname = applicationDataPath + "\\SnoopLog.txt";

	if (!append)    
	{    
		File::Delete(pathname);        
	}

	FileInfo ^ fi = gcnew FileInfo(pathname);
	            
	StreamWriter ^ sw = fi->AppendText();   
	sw->WriteLine(DateTime::Now.ToString("MM/dd/yyyy HH:mm:ss") + " : " + message);
	sw->Close();
}

__declspec(dllexport) 
LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam)
{
	if (nCode == HC_ACTION)
	{
		const auto msg = (CWPSTRUCT*)lparam;

		if (msg != nullptr 
			&& msg->message == WM_GOBABYGO)
		{
			Diagnostics::Debug::WriteLine("Got WM_GOBABYGO message");

			const auto acmRemote = (wchar_t*)msg->wParam;

			const auto acmLocal = gcnew String(acmRemote);
			Diagnostics::Debug::WriteLine(String::Format("acmLocal = {0}", acmLocal));

			auto serializer = gcnew Xml::Serialization::XmlSerializer(InjectorData::typeid);

			const auto stringReader = gcnew StringReader(acmLocal);
			const auto injectorData = static_cast<InjectorData^>(serializer->Deserialize(stringReader));

			Diagnostics::Debug::WriteLine(String::Format("About to load assembly {0}", injectorData->AssemblyName));
			auto assembly = Reflection::Assembly::LoadFile(injectorData->AssemblyName);

			if (assembly != nullptr)
			{
				Diagnostics::Debug::WriteLine(String::Format("About to load type {0}", injectorData->ClassName));
				auto type = assembly->GetType(injectorData->ClassName);
				if (type != nullptr)
				{
					Diagnostics::Debug::WriteLine(String::Format("Just loaded the type {0}", injectorData->ClassName));

					Diagnostics::Debug::WriteLine(String::Format("About to get method info for {0}", injectorData->MethodName));
					auto methodInfo = type->GetMethod(injectorData->MethodName, Reflection::BindingFlags::Static | Reflection::BindingFlags::Public);

					if (methodInfo != nullptr)
					{
						Diagnostics::Debug::WriteLine(String::Format("Just got method info for {0}", injectorData->MethodName));

						Diagnostics::Debug::WriteLine(String::Format("About to invoke {0} on type {1}", methodInfo->Name, injectorData->ClassName));
						auto args = gcnew array<Object^>(1);
						args[0] = injectorData->SettingsFile;
						auto returnValue = methodInfo->Invoke(nullptr, args);

						if (nullptr == returnValue)
						{
							returnValue = "NULL";
						}
						Diagnostics::Debug::WriteLine(String::Format("Return value of {0} on type {1} is {2}", methodInfo->Name, injectorData->ClassName, returnValue));
					}
				}
			}
		}
	}

	return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
}