#include "pch.h"
#include "NetCoreApp3_0Executor.h"

#include "mscoree.h"

typedef HRESULT(STDAPICALLTYPE* FnGetNETCoreCLRRuntimeHost)(REFIID riid, IUnknown** pUnk);

// Returns the ICLRRuntimeHost instance or nullptr on failure.
ICLRRuntimeHost* GetNETCoreCLRRuntimeHost()
{
	const auto coreCLRModule = ::GetModuleHandle(L"coreclr.dll");

	if (!coreCLRModule)
	{
		return nullptr;
	}

	const auto pfnGetCLRRuntimeHost = FnGetNETCoreCLRRuntimeHost(::GetProcAddress(coreCLRModule, "GetCLRRuntimeHost"));
	if (!pfnGetCLRRuntimeHost)
	{
		return nullptr;
	}

	ICLRRuntimeHost* clrRuntimeHost = nullptr;
	const auto hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost, reinterpret_cast<IUnknown**>(&clrRuntimeHost));
	if (FAILED(hr)) {
		return nullptr;
	}

	return clrRuntimeHost;
}

int NetCoreApp3_0Executor::Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName,	LPCWSTR pwzArgument, DWORD* pReturnValue)
{
	auto host = GetNETCoreCLRRuntimeHost();

	if (!host)
	{
		OutputDebugString(L"Could not get runtime host.");
		return E_FAIL;
	}

	const auto hr = host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue);

	host->Release();

	return hr;
}
