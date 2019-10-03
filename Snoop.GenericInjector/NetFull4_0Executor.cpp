#include "pch.h"
#include "NetFull4_0Executor.h"

#include <metahost.h>
#include "mscoree.h"
#pragma comment(lib, "mscoree.lib")

ICLRRuntimeHost* GetNETFullCLRRuntimeHost()
{
	ICLRMetaHost* metaHost = nullptr;
	ICLRRuntimeInfo* runtimeInfo = nullptr;
	ICLRRuntimeHost* runtimeHost = nullptr;

	if (CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, reinterpret_cast<LPVOID*>(&metaHost)) == S_OK)
	{
		if (metaHost->GetRuntime(L"v4.0.30319", IID_ICLRRuntimeInfo, reinterpret_cast<LPVOID*>(&runtimeInfo)) == S_OK)
		{
			runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, reinterpret_cast<LPVOID*>(&runtimeHost));
		}

		runtimeInfo->Release();
		metaHost->Release();
	}

	return runtimeHost;
}

int NetFull4_0Executor::Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue)
{
	auto host = GetNETFullCLRRuntimeHost();

	if (!host)
	{
		OutputDebugString(L"Could not get runtime host.");
		return E_FAIL;
	}

	const auto hr = host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue);

	host->Release();

	return hr;
}
