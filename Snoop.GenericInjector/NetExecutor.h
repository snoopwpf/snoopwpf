#pragma once

#include "pch.h"
#include "mscoree.h"

#include "FrameworkExecutor.h"

class NetExecutor final : public FrameworkExecutor
{
public:
	NetExecutor() : FrameworkExecutor(L"NetExecutor")
	{}
	
	int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) override;
private:
	ICLRRuntimeHost* GetNETCoreCLRRuntimeHost();
};