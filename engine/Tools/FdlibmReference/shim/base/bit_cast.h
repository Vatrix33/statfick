// A stand-in for Chromium's base/bit_cast.h: just what Blink's fdlibm uses (Tools/FdlibmReference/reference.cc).
#pragma once
#include <cstring>
namespace base { template <class D, class S> D bit_cast(const S& s) { D d; std::memcpy(&d, &s, sizeof d); return d; } }
