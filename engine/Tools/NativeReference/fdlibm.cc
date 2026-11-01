// The fdlibm reference (Docs/Audio.md): Blink's own fdlibm (third_party/fdlibm/ieee754.cc as
// Chromium 152 builds it for Web Audio, built here against small stand-ins for the Chromium headers
// it includes, in shim/) evaluated on inputs like Web Audio's: exp for setTargetAtTime and the
// discrete time constants, pow for detune and exponential ramps. Prints JSON: inputs and results as
// the hex of their IEEE bits; sin, cos, log and expf for the biquad's coefficients and tail. Built and run by Tools/Scripts/make-native-references.sh.
#include <cstdint>
#include <cstdio>
#include <cstring>

#include "third_party/fdlibm/ieee754.h"

namespace {

uint32_t state = 12345;

// The composer's mulberry32, as the C# test reads nothing but this file's output.
double Random() {
  state += 0x6D2B79F5u;
  uint32_t t = state;
  t = (t ^ (t >> 15)) * (t | 1u);
  t ^= t + (t ^ (t >> 7)) * (t | 61u);
  return (t ^ (t >> 14)) / 4294967296.0;
}

float Fround(double x) { return static_cast<float>(x); }

unsigned long long Bits(double d) {
  unsigned long long b;
  std::memcpy(&b, &d, sizeof b);
  return b;
}

unsigned FloatBits(float f) {
  unsigned b;
  std::memcpy(&b, &f, sizeof b);
  return b;
}

}  // namespace

int main() {
  const int kCases = 1500;
  std::printf("{\"exp\":[");
  for (int i = 0; i < kCases; i++) {
    const int kind = i % 3;
    const double x = kind == 0   ? -Random() * 20
                     : kind == 1 ? (Random() - 0.5) * 2
                                 : -1 / (48000 * (Random() * 0.5 + 1e-4));
    std::printf("%s[\"%016llx\",\"%016llx\"]", i ? "," : "", Bits(x), Bits(fdlibm::exp(x)));
  }
  std::printf("],\"pow\":[");
  for (int i = 0; i < kCases; i++) {
    const int kind = i % 3;
    double x, y;
    if (kind == 0) {
      x = 2;
      y = Fround((Random() - 0.5) * 2400) / 1200.0f;
    } else if (kind == 1) {
      x = Random() * 50 + 1e-3;
      y = (Random() - 0.5) * 4;
    } else {
      const float a = Fround(Random() * 2);
      const float b = Fround(Random() * 2 + 0.01);
      x = a / b;
      y = 1 / (Random() * 48000 + 1);
    }
    std::printf("%s[\"%016llx\",\"%016llx\",\"%016llx\"]", i ? "," : "", Bits(x), Bits(y), Bits(fdlibm::pow(x, y)));
  }
  std::printf("],\"sin\":[");
  for (int i = 0; i < kCases; i++) {
    const int kind = i % 3;
    const double x = kind == 0 ? 3.14159265358979323846 * Random() : kind == 1 ? (Random() - 0.5) * 20 : 3.14159265358979323846 * Fround(Random());
    std::printf("%s[\"%016llx\",\"%016llx\",\"%016llx\"]", i ? "," : "", Bits(x), Bits(fdlibm::sin(x)), Bits(fdlibm::cos(x)));
  }
  std::printf("],\"log\":[");
  for (int i = 0; i < kCases; i++) {
    const int kind = i % 3;
    const double x = kind == 0 ? 1 - Random() * 0.01 : kind == 1 ? Random() * 1000 : 1 / 32768.0 / (Random() * 4 + 1e-6);
    std::printf("%s[\"%016llx\",\"%016llx\"]", i ? "," : "", Bits(x), Bits(fdlibm::log(x)));
  }
  std::printf("],\"expf\":[");
  for (int i = 0; i < kCases; i++) {
    const float x = Fround((Random() - 0.5) * 80);
    std::printf("%s[\"%08x\",\"%08x\"]", i ? "," : "", FloatBits(x), FloatBits(fdlibm::expf(x)));
  }
  std::printf("]}\n");
}
