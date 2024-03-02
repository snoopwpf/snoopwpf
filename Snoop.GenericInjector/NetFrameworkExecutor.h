#pragma once

#include "pch.h"

#include <metahost.h>

#include "FrameworkExecutor.h"

class NetFrameworkExecutor final : public FrameworkExecutor
{
public:
	NetFrameworkExecutor() : FrameworkExecutor(L"NetFull4_0Executor")
	{}
	
	int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) override;

private:
	ICLRRuntimeHost* GetNETFullCLRRuntimeHost();
};
