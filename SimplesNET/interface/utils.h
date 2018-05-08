#pragma once
#include <comdef.h>
#include <string>

using namespace std;

namespace utils
{
	ref class Utils
	{
	public:
		template<typename T>
		static cli::array<T^>^ ConcatArrays(cli::array<T^>^ ar1, cli::array<T^>^ ar2)
		{
			if (ar1 == nullptr)
				return ar2;
			cli::array<T^>^ _new = ar1;
			cli::array<T^>^ _newBig = gcnew cli::array<T^>(ar1->Length + ar2->Length);
			if (ar1->Length)
				ar1->CopyTo(_newBig, 0);
			if (ar2->Length)
				ar2->CopyTo(_newBig, ar1->Length - 1);
			return _newBig;
		}
	};
}

namespace cli_strings
{
	inline const _bstr_t	string2bstr(System::String^const _str){
		if (System::String::IsNullOrEmpty(_str))
			return "";
		_bstr_t _result;
		_result.Attach( (BSTR)(void*)System::Runtime::InteropServices::Marshal::StringToBSTR(_str) );
		return _result;
	}

	template<class char_t>
	inline std::basic_string<char_t, std::char_traits<char_t>, std::allocator<char_t> >	string2stdString_t(System::String^const _str){
		typedef std::basic_string<char_t, std::char_traits<char_t>, allocator<char_t> >	string_t;
		const _bstr_t _result = string2bstr(_str);
		return !_result.length() ? string_t() : string_t((char_t*)_result);		
	}

	inline std::basic_string<TCHAR, std::char_traits<TCHAR>, std::allocator<TCHAR> >	string2stdString(System::String^const _str){
		return string2stdString_t<TCHAR>(_str);
	}
	
	inline System::String^	bstr2string(const BSTR _str){
		if (!_str)
			return System::String::Empty;
		return System::Runtime::InteropServices::Marshal::PtrToStringBSTR((System::IntPtr)(void*)_str);
	}

	inline System::String^	stdString2string(const std::string& _str){
		return bstr2string(_bstr_t(_str.c_str()));
	}

	inline System::String^	bstr2string(const _bstr_t& _str){
		return bstr2string(static_cast<BSTR>(_str));
	}

	inline System::String^	sz2string(const char* _sz){
		return bstr2string(_bstr_t(std::string(_sz).c_str()));
	}

};
/*
namespace cli_variant
{
	inline System::Object^	variant2object(const VARIANT _var){
		return System::Runtime::InteropServices::Marshal::GetObjectForNativeVariant(System::IntPtr((void*)&_var));
	}
	inline _variant_t	object2variant(System::Object^const _obj){
		_variant_t _var;
		System::Runtime::InteropServices::Marshal::GetNativeVariantForObject(_obj,System::IntPtr((void*)&_var));
		return _var;
	}
};*/