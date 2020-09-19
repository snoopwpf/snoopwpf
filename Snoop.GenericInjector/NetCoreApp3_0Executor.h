#pragma once

#include "pch.h"
#include "mscoree.h"

#include "FrameworkExecutor.h"

class NetCoreApp3_0Executor final : public FrameworkExecutor
{
public:
	NetCoreApp3_0Executor() : FrameworkExecutor(L"NetCoreApp3_0Executor")
	{}
	
	int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) override;
private:
	ICLRRuntimeHost* GetNETCoreCLRRuntimeHost();
};