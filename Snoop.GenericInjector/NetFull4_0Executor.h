#pragma once
#include "pch.h"
#include "FrameworkExecutor.h"

class NetFull4_0Executor : public FrameworkExecutor
{
public:
	int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) override;
};
