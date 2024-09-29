#include "pch.h"
#include "LogHelper.h"

#include <comdef.h>

#include "NetExecutor.h"
#ifndef NO_FULL_FRAMEWORK
#include "NetFrameworkExecutor.h"
#endif

std::unique_ptr<FrameworkExecutor> GetExecutor(const std::wstring& framework)
{
	LogHelper::WriteLine(L"Trying to get executor for framework '%s'...", framework.c_str());

	if (icase_cmp(framework, L"net6.0-windows"))
	{
		return std::make_unique<NetExecutor>();
	}

#ifndef NO_FULL_FRAMEWORK
	if (icase_cmp(framework, L"net462"))
	{
		return std::make_unique<NetFrameworkExecutor>();
	}
#endif

	LogHelper::WriteLine(L"Framework '%s' is not supported.", framework.c_str());

	return nullptr;
}

extern "C" __declspec(dllexport) int STDMETHODVCALLTYPE ExecuteInDefaultAppDomain(const LPCWSTR input)
{
	try
	{
		LogHelper::WriteLine(input);
		const auto parts = split(input, L"<|>");

		if (parts.size() < 6)
		{
			LogHelper::WriteLine(L"Not enough parameters.");
			return E_INVALIDARG;
		}

		const auto& framework = parts.at(0);
		const auto& assemblyPath = parts.at(1);
		const auto& className = parts.at(2);
		const auto& method = parts.at(3);
		const auto& parameter = parts.at(4);
		const auto& logFile = parts.at(5);

		LogHelper::SetLogFile(logFile);
		LogHelper::WriteLine(input);

		const auto executor = GetExecutor(framework);

		if (!executor)
		{
			LogHelper::WriteLine(L"No executor found.");
			return E_NOTIMPL;
		}

		DWORD* retVal = nullptr;
		const auto hr = executor->Execute(assemblyPath.c_str(), className.c_str(), method.c_str(), parameter.c_str(), retVal);

		if (FAILED(hr))
		{
			const _com_error err(hr);

			LogHelper::WriteLine(L"Error while calling '%s' on '%s' from '%s' with '%s'", method.c_str(), className.c_str(), assemblyPath.c_str(), parameter.c_str());
			LogHelper::WriteLine(L"HResult: %i", hr);
			LogHelper::WriteLine(L"Message: %s", err.ErrorMessage());
			LogHelper::WriteLine(L"Description: %s", std::wstring(err.Description(), SysStringLen(err.Description())).c_str());
		}

		return hr;
	}
	catch (std::exception& exception)
	{
		LogHelper::WriteLine(L"ExecuteInDefaultAppDomain failed with exception.");
		LogHelper::WriteLine(L"Exception:");
		LogHelper::WriteLine(to_wstring(exception.what()));
	}
	catch (...)
	{
		LogHelper::WriteLine(L"ExecuteInDefaultAppDomain failed with unknown exception.");
	}

	return E_FAIL;
}