#include "pch.h"
#include "NetFrameworkExecutor.h"

#include <metahost.h>
#include "mscoree.h"
#pragma comment(lib, "mscoree.lib")

ICLRRuntimeHost* NetFrameworkExecutor::GetNETFullCLRRuntimeHost()
{
	ICLRMetaHost* metaHost = nullptr;
	ICLRRuntimeInfo* runtimeInfo = nullptr;
	ICLRRuntimeHost* runtimeHost = nullptr;

	this->Log(L"Trying to get runtime meta host...");

	if (CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, reinterpret_cast<LPVOID*>(&metaHost)) == S_OK)
	{
		this->Log(L"Got runtime meta host.");

		this->Log(L"Trying to get runtime info...");

		if (metaHost->GetRuntime(L"v4.0.30319", IID_ICLRRuntimeInfo, reinterpret_cast<LPVOID*>(&runtimeInfo)) == S_OK)
		{
			this->Log(L"Got runtime info.");

			this->Log(L"Trying to get runtime host...");

			runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, reinterpret_cast<LPVOID*>(&runtimeHost));

			if (runtimeHost)
			{
				this->Log(L"Got runtime host.");
			}
			else
			{
				this->Log(L"Could not get runtime host.");
			}
		}
		else
		{
			this->Log(L"Could not get runtime info.");
		}

		runtimeInfo->Release();
		metaHost->Release();
	}
	else
	{
		this->Log(L"Could not get runtime meta host.");
	}

	return runtimeHost;
}

int NetFrameworkExecutor::Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue)
{
	auto host = GetNETFullCLRRuntimeHost();

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
