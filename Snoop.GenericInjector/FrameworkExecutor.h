#pragma once
#include "pch.h"
#include "LogHelper.h"

class FrameworkExecutor
{
public:
	FrameworkExecutor(const std::wstring& executorName)
		: name(executorName)
	{
		logPrefix = this->name + L": ";
	}

	FrameworkExecutor(const FrameworkExecutor&) = delete;
	virtual ~FrameworkExecutor() = default;
	virtual int Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName, LPCWSTR pwzArgument, DWORD* pReturnValue) = 0;
	
protected:
	template<typename ... Args>
	void Log(const std::wstring& format, Args ... args)
	{
		const auto output = string_format(this->logPrefix + format, args...);
		LogHelper::WriteLine(output);
	}
	
	std::wstring name;
	std::wstring logPrefix;
};