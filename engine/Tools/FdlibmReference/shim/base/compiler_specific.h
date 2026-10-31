// A stand-in for Chromium's base/compiler_specific.h: just what Blink's fdlibm uses (Tools/FdlibmReference/reference.cc).
#pragma once
#define ALWAYS_INLINE inline __attribute__((always_inline))
