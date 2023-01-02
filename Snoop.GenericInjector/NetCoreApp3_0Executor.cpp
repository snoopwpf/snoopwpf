#include "pch.h"
#include "NetCoreApp3_0Executor.h"

#include <windows.h>
#include "mscoree.h"

#include "Psapi.h"

#include <optional>
#include <algorithm>

// Returns the ICLRRuntimeHost instance or nullptr on failure.
ICLRRuntimeHost* NetCoreApp3_0Executor::GetNETCoreCLRRuntimeHost()
{
	FnGetNETCoreCLRRuntimeHost pfnGetCLRRuntimeHost;

	if (TryGetHandleForGetCLRRuntimeHostFromModule(GetModuleHandleForCoreClrDll(), pfnGetCLRRuntimeHost) == false)
	{
		if (IsSelfContained() == false)
		{
			return nullptr;
		}

		if (TryGetHandleForGetCLRRuntimeHostFromModule(::GetModuleHandle(nullptr), pfnGetCLRRuntimeHost) == false)
		{
			if (TryGetHandleForGetCLRRuntimeHostFromSelfContainedProcess(pfnGetCLRRuntimeHost) == false)
			{
				return nullptr;
			}
		}
	}

	this->Log(L"Trying to get runtime host...");

	ICLRRuntimeHost* clrRuntimeHost = nullptr;
	const auto hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost, reinterpret_cast<void**>(&clrRuntimeHost));
	
	if (FAILED(hr)) 
	{
		this->Log(L"Could not get runtime host.");
		return nullptr;
	}

	this->Log(L"Got runtime host.");

	return clrRuntimeHost;
}

int NetCoreApp3_0Executor::Execute(LPCWSTR pwzAssemblyPath, LPCWSTR pwzTypeName, LPCWSTR pwzMethodName,	LPCWSTR pwzArgument, DWORD* pReturnValue)
{
	auto* host = GetNETCoreCLRRuntimeHost();

	if (!host)
	{
		return E_FAIL;
	}

	this->Log(L"Trying to run ExecuteInDefaultAppDomain...");

	const auto hr = host->ExecuteInDefaultAppDomain(pwzAssemblyPath, pwzTypeName, pwzMethodName, pwzArgument, pReturnValue);

	this->Log(L"ExecuteInDefaultAppDomain finished.");

	host->Release();

	return hr;
}

HINSTANCE NetCoreApp3_0Executor::GetModuleHandleForCoreClrDll()
{
	this->Log(L"Getting handle for coreclr.dll...");

	auto* const coreCLRModule = ::GetModuleHandle(L"coreclr.dll");

	if (!coreCLRModule)
	{
		this->Log(L"Could not get handle for coreclr.dll.");
		return coreCLRModule;
	}

	this->Log(L"Got handle for coreclr.dll.");
	return coreCLRModule;
}

bool NetCoreApp3_0Executor::TryGetHandleForGetCLRRuntimeHostFromModule(const HINSTANCE moduleHandle, FnGetNETCoreCLRRuntimeHost& pfnGetCLRRuntimeHost)
{
	if (!moduleHandle)
	{
		return false;
	}

	this->Log(L"Getting proc address for GetCLRRuntimeHost...");

	pfnGetCLRRuntimeHost = reinterpret_cast<FnGetNETCoreCLRRuntimeHost>(::GetProcAddress(moduleHandle, "GetCLRRuntimeHost"));
	if (!pfnGetCLRRuntimeHost)
	{
		this->Log(L"Could not get proc address for GetCLRRuntimeHost.");
		return false;
	}

	this->Log(L"Got proc address for GetCLRRuntimeHost.");
	return true;
}

bool NetCoreApp3_0Executor::IsSelfContained()
{
	this->Log(L"Checking for self contained executable...");

	const auto currentProcessModule = ::GetModuleHandle(nullptr);
	const auto procAddress = ::GetProcAddress(currentProcessModule, "DotNetRuntimeInfo");
	if (procAddress)
	{
		this->Log(L"Executable is self contained. (DotNetRuntimeInfo at address %ul)", procAddress);
		return true;
	}

	this->Log(L"Executable is NOT self contained.");
	return false;
}

bool BinaryCompare(PBYTE pData, PBYTE bMask, PCHAR szMask)
{
    for(;*szMask; ++szMask, ++pData, ++bMask)
    {
        if(*szMask=='x' && *pData!=*bMask)
        {
            return false;
		}
	}
    return (*szMask) == NULL;
}

DWORD_PTR FindPattern(DWORD_PTR dwAddress, DWORD dwLen, PBYTE bMask, PCHAR szMask)
{
    for(DWORD i=0; i<dwLen; i++)
    {
        if (BinaryCompare((PBYTE)(dwAddress+i),bMask,szMask))
        {
            return (DWORD_PTR)(dwAddress+i);
		}
	}
    return 0;
}

char const* FindPatternViaArray(char const* const  beg, char const* const end)
{
#ifdef X64
	static std::optional<char> const m[] = {
		{'\x48'}, {'\x89'}, {'\x5c'}, {'\x24'},	{'\x08'}, {'\x57'}, {'\x48'}, {'\x83'},
		{'\xec'}, {'\x20'}, {'\xb9'}, {'\x20'},	{'\x00'}, {'\x00'}, {'\x00'}, {'\x48'},
		{'\x8b'}, {'\xfa'}, {'\xe8'}, {'\x79'},	{'\x00'}, {'\x29'}, {'\x00'}, {'\x48'}};
#else
		//56 57 8B F2 A1 18 27 A9  00 85 C0 75 18 FF 15 A0
		//41 94 00 8B C8 BA 18 27  A9 00 33 C0 F0 0F B1 0A
		static std::optional<char> const m[] = {
		{'\x56'}, {'\x57'}, {'\x8B'}, {'\xF2'},	{'\xA1'}, {'\x18'}, {'\x27'}, {'\xA9'},
		{'\x00'}, {'\x85'}, {'\xC0'}, {'\x75'},	{'\x18'}, {'\xFF'}, {'\x15'}, {'\xA0'},
		{'\x41'}, {'\x94'}, {'\x00'}, {'\x8B'},	{'\xC8'}, {'\xBA'}, {'\x18'}, {'\x27'}};
#endif

    return std::search(beg, end, std::begin(m), std::end(m), [](auto a, auto b){
        if(!b.has_value())
            return true;
        return a == b.value();
    });
}

bool NetCoreApp3_0Executor::TryGetHandleForGetCLRRuntimeHostFromSelfContainedProcess(FnGetNETCoreCLRRuntimeHost& pfnGetCLRRuntimeHost)
{
	this->Log(L"Searching for GetCLRRuntimeHost in self contained process...");

	pfnGetCLRRuntimeHost = nullptr;

	const auto currentProcessModule = ::GetModuleHandle(nullptr);

	this->Log(L"currentProcessModule: %ul", currentProcessModule);

	{
		// This will get the DLL base address (which can vary)
	    HMODULE hMod = currentProcessModule;

	    // Get module info
	    MODULEINFO modinfo = { NULL, };
	    GetModuleInformation( GetCurrentProcess(), hMod, &modinfo, sizeof(modinfo) );

	    // This will search the module for the address of a given signature
#if X64
	    const auto funcAddressGuessed = FindPattern(
	        DWORD_PTR(currentProcessModule), modinfo.SizeOfImage,
	        (PBYTE)"\x48\x89\x5c\x24\x08\x57\x48\x83\xec\x20\xb9\x20\x00\x00\x00\x48\x8b\xfa\xe8\x79\x00\x29\x00\x48",
	        PCHAR("xxxxxxxxxxxxxxxxxxxxxxxx")
	    );
#else
		//56 57 8B F2 A1 18 27 A9  00 85 C0 75 18 FF 15 A0
		//41 94 00 8B C8 BA 18 27  A9 00 33 C0 F0 0F B1 0A
	    const auto funcAddressGuessed = FindPattern(
	        DWORD_PTR(currentProcessModule), modinfo.SizeOfImage,
	        (PBYTE)"\x56\x57\x8B\xF2\xA1\x18\x27\xA9\x00\x85\xC0\x75\x18\xFF\x15\xA0\x41\x94\x00\x8B\xC8\xBA\x18\x27",
	        PCHAR("xxxxxxxxxxxxxxxxxxxxxxxx")
	    );
#endif

		this->Log(L"funcAddressGuessed:   %ul", funcAddressGuessed);
	}

	// This will get the DLL base address (which can vary)
	HMODULE hMod = currentProcessModule;

	// Get module info
	MODULEINFO modinfo = { NULL, };
	GetModuleInformation( GetCurrentProcess(), hMod, &modinfo, sizeof(modinfo) );

	// This will search the module for the address of a given signature
	const auto funcAddressGuessed = FindPatternViaArray(
		(char*)currentProcessModule, (char*)currentProcessModule + modinfo.SizeOfImage);

	this->Log(L"funcAddressGuessed:   %ul", funcAddressGuessed);

	// todo: guessing currently only works on x64, but fails for x86. ARM and ARM64 also would need some investigation.
	const auto funcAddress = (DWORD_PTR)funcAddressGuessed;
	//const auto funcAddress = ((DWORD_PTR)currentProcessModule + 0x456B0); //x64
	//const auto funcAddress = ((DWORD_PTR)currentProcessModule + XXXXXXX); //x86
	
	this->Log(L"funcAddress:          %ul", funcAddress);

	pfnGetCLRRuntimeHost = reinterpret_cast<FnGetNETCoreCLRRuntimeHost>(funcAddress);

	if (!pfnGetCLRRuntimeHost)
	{
		this->Log(L"Could not get func address for GetCLRRuntimeHost.");
		return false;
	}

	this->Log(L"Got func address GetCLRRuntimeHost. (%ul)", funcAddress);
	return true;
}