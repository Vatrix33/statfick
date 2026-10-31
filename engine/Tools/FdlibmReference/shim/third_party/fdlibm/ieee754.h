// A stand-in for Chromium's third_party/fdlibm/ieee754.h: just what Blink's fdlibm uses (Tools/FdlibmReference/reference.cc).
#pragma once
namespace fdlibm { double exp(double); double pow(double, double); double expm1(double); double log(double); double sin(double); double cos(double);
double acos(double); double acosh(double); double asin(double); double asinh(double); double atan(double); double atan2(double,double); double atanh(double);
double log1p(double); double log2(double); double log10(double); double cbrt(double); double tan(double); double sinh(double); double cosh(double); double tanh(double); }
