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

	OutputDebugString(L"NetFull4_0Executor: Trying to get runtime meta host...");

	if (CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, reinterpret_cast<LPVOID*>(&metaHost)) == S_OK)
	{
		OutputDebugString(L"NetFull4_0Executor: Got runtime meta host.");

		OutputDebugString(L"NetFull4_0Executor: Trying to get runtime info...");

		if (metaHost->GetRuntime(L"v4.0.30319", IID_ICLRRuntimeInfo, reinterpret_cast<LPVOID*>(&runtimeInfo)) == S_OK)
		{
			OutputDebugString(L"NetFull4_0Executor: Got runtime info.");

			OutputDebugString(L"NetFull4_0Executor: Trying to get runtime host...");

			runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, reinterpret_cast<LPVOID*>(&runtimeHost));

			if (runtimeHost)
			{
				OutputDebugString(L"NetFull4_0Executor: Got runtime host.");
			}
			else
			{
				OutputDebugString(L"NetFull4_0Executor: Could not get runtime host.");
			}
		}
		else
		{
			OutputDebugString(L"NetFull4_0Executor: Could not get runtime info.");
		}
		

		runtimeInfo->Release();
		metaHost->Release();
	}
	else
	{
		OutputDebugString(L"NetFull4_0Executor: Could not get runtime meta host.");
	}

	return runtimeHost;
}

int NetFull4_0Executor::Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue)
{
	auto host = GetNETFullCLRRuntimeHost();

	if (!host)
	{
		return E_FAIL;
	}

	OutputDebugString(L"NetFull4_0Executor: Trying to ExecuteInDefaultAppDomain...");

	const auto hr = host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue);

	OutputDebugString(L"NetFull4_0Executor: ExecuteInDefaultAppDomain finished.");

	host->Release();

	return hr;
}
