#pragma once

#include "pch.h"

class LogHelper {
public:
    template<typename ... Args>
    static void WriteLine(const std::wstring& format, Args ... args)
    {
    	const auto output = string_format(format, args...);
    	OutputDebugString(output.c_str());
    	std::wcout << output << std::endl;
    }
};
