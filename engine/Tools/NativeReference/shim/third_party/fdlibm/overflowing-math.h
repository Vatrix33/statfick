// A stand-in for Chromium's third_party/fdlibm/overflowing-math.h: just what Blink's fdlibm uses (Tools/NativeReference/fdlibm.cc).
#pragma once
#include <cstdint>
#include <type_traits>
namespace fdlibm { template <typename T> inline T AddWithWraparound(T a, T b) { using U = typename std::make_unsigned<T>::type; return static_cast<T>(static_cast<U>(a) + static_cast<U>(b)); }
template <typename T> inline T SubWithWraparound(T a, T b) { using U = typename std::make_unsigned<T>::type; return static_cast<T>(static_cast<U>(a) - static_cast<U>(b)); }
inline double Divide(double a, double b) { return a / b; } }
namespace fdlibm { template <typename T> inline T NegateWithWraparound(T a) { using U = typename std::make_unsigned<T>::type; return static_cast<T>(-static_cast<U>(a)); } }
