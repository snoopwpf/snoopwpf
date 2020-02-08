#include "pch.h"
#include "NetCoreApp3_0Executor.h"

#include "mscoree.h"

typedef HRESULT(STDAPICALLTYPE* FnGetNETCoreCLRRuntimeHost)(REFIID riid, IUnknown** pUnk);

// Returns the ICLRRuntimeHost instance or nullptr on failure.
ICLRRuntimeHost* GetNETCoreCLRRuntimeHost()
{
	OutputDebugStringEx(L"NetCoreApp3_0Executor: Getting handle for coreclr.dll...");
	const auto coreCLRModule = ::GetModuleHandle(L"coreclr.dll");

	if (!coreCLRModule)
	{
		OutputDebugStringEx(L"NetCoreApp3_0Executor: Could not get handle for coreclr.dll.");
		return nullptr;
	}

	OutputDebugStringEx(L"NetCoreApp3_0Executor: Got handle for coreclr.dll.");

	OutputDebugString(L"NetCoreApp3_0Executor: Getting handle for GetCLRRuntimeHost...");

	const auto pfnGetCLRRuntimeHost = FnGetNETCoreCLRRuntimeHost(::GetProcAddress(coreCLRModule, "GetCLRRuntimeHost"));
	if (!pfnGetCLRRuntimeHost)
	{
		OutputDebugStringEx(L"NetCoreApp3_0Executor: Could not get handle for GetCLRRuntimeHost.");
		return nullptr;
	}

	OutputDebugStringEx(L"NetCoreApp3_0Executor: Got handle for GetCLRRuntimeHost.");

	OutputDebugStringEx(L"NetCoreApp3_0Executor: Trying to get runtime host...");

	ICLRRuntimeHost* clrRuntimeHost = nullptr;
	const auto hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost, reinterpret_cast<IUnknown**>(&clrRuntimeHost));
	
	if (FAILED(hr)) 
	{
		OutputDebugStringEx(L"NetCoreApp3_0Executor: Could not get runtime host.");
		return nullptr;
	}

	OutputDebugStringEx(L"NetCoreApp3_0Executor: Got runtime host.");

	return clrRuntimeHost;
}

int NetCoreApp3_0Executor::Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName,	LPCWSTR pwzArgument, DWORD* pReturnValue)
{
	auto host = GetNETCoreCLRRuntimeHost();

	if (!host)
	{
		return E_FAIL;
	}

	OutputDebugStringEx(L"NetCoreApp3_0Executor: Trying to ExecuteInDefaultAppDomain...");

	const auto hr = host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue);

	OutputDebugStringEx(L"NetCoreApp3_0Executor: ExecuteInDefaultAppDomain finished.");

	host->Release();

	return hr;
}
