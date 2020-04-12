#include "pch.h"
#include <array>
#include <string>
#include <vector>

#include <comdef.h>

#include "NetCoreApp3_0Executor.h"
#include "NetFull4_0Executor.h"

bool icase_wchar_cmp(const wchar_t a, const wchar_t b)
{
	return std::tolower(a) == std::tolower(b);
}

bool icase_cmp(std::wstring const& s1, std::wstring const& s2)
{
	return s1.size() == s2.size()
		   && std::equal(s1.begin(), s1.end(), s2.begin(), icase_wchar_cmp);
}

std::vector<std::wstring> split(const std::wstring& input, const std::wstring& delimiter)
{
	std::vector<std::wstring> parts;
	std::wstring::size_type startIndex = 0;
	std::wstring::size_type endIndex;
	
	while ((endIndex = input.find(delimiter, startIndex)) < input.size())
	{
		auto val = input.substr(startIndex, endIndex - startIndex);
		parts.push_back(val);
		startIndex = endIndex + delimiter.size();
	}
	
	if (startIndex < input.size())
	{
		const auto val = input.substr(startIndex);
		parts.push_back(val);
	}
	
	return parts;
}

std::unique_ptr<FrameworkExecutor> GetExecutor(const std::wstring& framework)
{
	OutputDebugStringEx(L"Trying to get executor for framework '%s'...", framework.c_str());
	
	if (icase_cmp(framework, L"netcoreapp3.0")
		|| icase_cmp(framework, L"netcoreapp3.1"))
	{
		return std::make_unique<NetCoreApp3_0Executor>();
	}

	if (icase_cmp(framework, L"net40"))
	{
		return std::make_unique<NetFull4_0Executor>();
	}

	OutputDebugStringEx(L"Framework '%s' is not supported.", framework.c_str());
	
	return nullptr;
}

extern "C" __declspec(dllexport) int STDMETHODVCALLTYPE ExecuteInDefaultAppDomain(const LPCWSTR input)
{
	try
	{
		OutputDebugStringEx(input);
		const auto parts = split(input, L"<|>");

		if (parts.size() != 5)
		{
			OutputDebugStringEx(L"Not enough parameters.");
			return E_INVALIDARG;
		}

		const auto& framework = parts.at(0);
		const auto& assemblyPath = parts.at(1);
		const auto& className = parts.at(2);
		const auto& method = parts.at(3);
		const auto& parameter = parts.at(4);

		const auto executor = GetExecutor(framework);

		if (!executor)
		{
			OutputDebugStringEx(L"No executor found.");
			return E_NOTIMPL;
		}
		
		DWORD* retVal = nullptr;
		const auto hr = executor->Execute(assemblyPath.c_str(), className.c_str(), method.c_str(), parameter.c_str(), retVal);
			
		if (FAILED(hr))
		{
			const _com_error err(hr);

			OutputDebugStringEx(L"Error while calling '%s' on '%s' from '%s' with '%s'", method.c_str(), className.c_str(), assemblyPath.c_str(), parameter.c_str());
			OutputDebugStringEx(L"HResult: %i", hr);
			OutputDebugStringEx(L"Message: %s", err.ErrorMessage());
			OutputDebugStringEx(L"Description: %s", std::wstring(err.Description(), SysStringLen(err.Description())).c_str());
		}
		
		return hr;
	}
	catch (std::exception& exception)
	{
		OutputDebugStringEx(L"ExecuteInDefaultAppDomain failed with exception.");
		OutputDebugStringEx(L"Exception:");
		OutputDebugStringEx(to_wstring(exception.what()));
	}
	catch (...)
	{
		OutputDebugStringEx(L"ExecuteInDefaultAppDomain failed with unknown exception.");
	}

	return E_FAIL;
}
