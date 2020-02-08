#pragma once
#include "pch.h"

class FrameworkExecutor
{
public:
	FrameworkExecutor() = default;
	FrameworkExecutor(const FrameworkExecutor&) = delete;
	virtual ~FrameworkExecutor() = default;
	virtual int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) = 0;
};