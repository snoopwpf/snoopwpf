#pragma once
#include "pch.h"

class FrameworkExecutor
{
public:
	virtual int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) = 0;
};