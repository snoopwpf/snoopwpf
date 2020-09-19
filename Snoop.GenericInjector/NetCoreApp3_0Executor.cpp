#include "pch.h"
#include "NetCoreApp3_0Executor.h"

#include "mscoree.h"

typedef HRESULT(STDAPICALLTYPE* FnGetNETCoreCLRRuntimeHost)(REFIID riid, IUnknown** pUnk);

// Returns the ICLRRuntimeHost instance or nullptr on failure.
ICLRRuntimeHost* NetCoreApp3_0Executor::GetNETCoreCLRRuntimeHost()
{
	this->Log(L"Getting handle for coreclr.dll...");
	const auto coreCLRModule = ::GetModuleHandle(L"coreclr.dll");

	if (!coreCLRModule)
	{
		this->Log(L"Could not get handle for coreclr.dll.");
		return nullptr;
	}

	this->Log(L"Got handle for coreclr.dll.");

	this->Log(L"Getting handle for GetCLRRuntimeHost...");

	const auto pfnGetCLRRuntimeHost = FnGetNETCoreCLRRuntimeHost(::GetProcAddress(coreCLRModule, "GetCLRRuntimeHost"));
	if (!pfnGetCLRRuntimeHost)
	{
		this->Log(L"Could not get handle for GetCLRRuntimeHost.");
		return nullptr;
	}

	this->Log(L"Got handle for GetCLRRuntimeHost.");

	this->Log(L"Trying to get runtime host...");

	ICLRRuntimeHost* clrRuntimeHost = nullptr;
	const auto hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost, reinterpret_cast<IUnknown**>(&clrRuntimeHost));
	
	if (FAILED(hr)) 
	{
		this->Log(L"Could not get runtime host.");
		return nullptr;
	}

	this->Log(L"Got runtime host.");

	return clrRuntimeHost;
}

int NetCoreApp3_0Executor::Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName,	LPCWSTR pwzArgument, DWORD* pReturnValue)
{
	auto host = GetNETCoreCLRRuntimeHost();

	if (!host)
	{
		return E_FAIL;
	}

	this->Log(L"Trying to ExecuteInDefaultAppDomain...");

	const auto hr = host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue);

	this->Log(L"ExecuteInDefaultAppDomain finished.");

	host->Release();

	return hr;
}
