#pragma once

#include "pch.h"
#include "FrameworkExecutor.h"

class NetCoreApp3_0Executor final : public FrameworkExecutor
{
public:
	int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) override;
};