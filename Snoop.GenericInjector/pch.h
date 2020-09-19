// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#pragma once

// add headers that you want to pre-compile here
#include <locale>
#include <memory>
#include <string>
#include <stdexcept>

#include "framework.h"

static std::wstring to_wstring(const std::string& input)
{
	std::wstring_convert<std::codecvt<wchar_t,char,std::mbstate_t>> conv;
	auto wstr = conv.from_bytes(input);
	return wstr;
}

template<typename ... Args>
static std::wstring string_format(const std::wstring& format, Args ... args)
{
	const size_t size = swprintf(nullptr, 0, format.c_str(), args...) + 1; // Extra space for '\0'
	
    if( size <= 0 )
    {
	    throw std::runtime_error("Error during formatting.");
    }

    const std::unique_ptr<wchar_t[]> buf(new wchar_t[size]);
    swprintf(buf.get(), size, format.c_str(), args...);
    return std::wstring(buf.get(), buf.get() + size - 1); // We don't want the '\0' inside
}

template<typename ... Args>
static void OutputDebugStringEx(const std::wstring& format, Args ... args)
{
	const auto output = string_format(format, args...);
	OutputDebugString(output.c_str());
}