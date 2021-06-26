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
#include <array>
#include <vector>
#include <iostream>
#include <stdexcept>

#include "framework.h"

static bool icase_wchar_cmp(const wchar_t a, const wchar_t b)
{
	return std::tolower(a) == std::tolower(b);
}

static bool icase_cmp(std::wstring const& s1, std::wstring const& s2)
{
	return s1.size() == s2.size()
		   && std::equal(s1.begin(), s1.end(), s2.begin(), icase_wchar_cmp);
}

static std::vector<std::wstring> split(const std::wstring& input, const std::wstring& delimiter)
{
	std::vector<std::wstring> parts;
	std::wstring::size_type startIndex = 0;
	std::wstring::size_type endIndex;
	
	while ((endIndex = input.find(delimiter, startIndex)) < input.size())
	{
		auto val = input.substr(startIndex, endIndex - startIndex);
		parts.push_back(val);
		startIndex = endIndex + delimiter.size();
	}
	
	if (startIndex < input.size())
	{
		const auto val = input.substr(startIndex);
		parts.push_back(val);
	}
	
	return parts;
}

static std::wstring to_wstring(const std::string& input)
{
	std::wstring_convert<std::codecvt<wchar_t,char,std::mbstate_t>> conv;
	auto wstr = conv.from_bytes(input);
	return wstr;
}

template<typename ... Args>
static std::wstring string_format(const std::wstring& format, Args ... args)
{
	if (format.empty())
	{
		return std::wstring();
	}

	const int size = swprintf(nullptr, 0, format.c_str(), args...);
	
    if (size <= 0)
    {
	    throw std::runtime_error("Error during formatting.");
    }

	const int adjustedSize = size + 1; // Extra space for '\0'

    const std::unique_ptr<wchar_t[]> buf(new wchar_t[adjustedSize]);
    swprintf(buf.get(), adjustedSize, format.c_str(), args...);
    return std::wstring(buf.get(), buf.get() + adjustedSize - 1); // We don't want the '\0' inside
}