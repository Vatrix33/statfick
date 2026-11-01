// The C library reference (Docs/Audio.md): this system's glibc exp2f and log2f, which Chromium on
// this PC calls for the oscillator's detune and the periodic waves' pitch ranges, and log10f and
// powf(10, y) (the decibel conversions; powf over every y with x = 10); and its double pow, which
// V8 calls for JavaScript's ** and Math.pow (step 6), over the argument families of the engine's
// Tests/.../Audio/JsMathTests.cs (MathArguments), drawn from mulberry32 in the same order: each
// family's result hash and first 8 results. Prints JSON: the
// FNV-1a hash of the results' bits over every float, and over every 4099th (the tests' sample), NaN
// results counted as one canonical NaN; and "unfusedDiffers": how many floats give another result
// with the same algorithm left unfused (glibc's SSE2 variant, compiled here) than glibc's own (its
// FMA variant on CPUs with FMA): 0 means the variant the CPU picks doesn't matter. Built and run by Tools/Scripts/make-native-references.sh.
#include <math.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

static uint32_t Bits(float f) {
  uint32_t u;
  memcpy(&u, &f, 4);
  return u;
}

static uint64_t Mix(uint64_t hash, float result) {
  uint32_t bits = isnan(result) ? 0x7fc00000u : Bits(result);
  for (int i = 0; i < 4; i++) {
    hash ^= (bits >> (8 * i)) & 0xff;
    hash *= 0x100000001b3ull;
  }
  return hash;
}

// glibc 2.43's sysdeps/ieee754/flt-32 exp2f and log2f with every a*b + c left unfused (the SSE2
// variant); the tables are the same as glibc's.
static const uint64_t kExp2Table[32] = {
    0x3ff0000000000000, 0x3fefd9b0d3158574, 0x3fefb5586cf9890f, 0x3fef9301d0125b51,
    0x3fef72b83c7d517b, 0x3fef54873168b9aa, 0x3fef387a6e756238, 0x3fef1e9df51fdee1,
    0x3fef06fe0a31b715, 0x3feef1a7373aa9cb, 0x3feedea64c123422, 0x3feece086061892d,
    0x3feebfdad5362a27, 0x3feeb42b569d4f82, 0x3feeab07dd485429, 0x3feea47eb03a5585,
    0x3feea09e667f3bcd, 0x3fee9f75e8ec5f74, 0x3feea11473eb0187, 0x3feea589994cce13,
    0x3feeace5422aa0db, 0x3feeb737b0cdc5e5, 0x3feec49182a3f090, 0x3feed503b23e255d,
    0x3feee89f995ad3ad, 0x3feeff76f2fb5e47, 0x3fef199bdd85529c, 0x3fef3720dcef9069,
    0x3fef5818dcfba487, 0x3fef7c97337b9b5f, 0x3fefa4afa2a490da, 0x3fefd0765b6e4540};

static float Unfused_exp2f(float x) {
  uint32_t ux = Bits(x);
  if (((ux >> 20) & 0x7ff) >= (Bits(128.0f) >> 20)) return exp2f(x); /* special cases: not compared */
  double xd = x, shift = 0x1.8p+52 / 32, kd = xd + shift;
  uint64_t ki;
  memcpy(&ki, &kd, 8);
  kd -= shift;
  double r = xd - kd;
  uint64_t t = kExp2Table[ki % 32] + (ki << 47);
  double s;
  memcpy(&s, &t, 8);
  double z = 0x1.c6af84b912394p-5 * r + 0x1.ebfce50fac4f3p-3, r2 = r * r;
  double y = 0x1.62e42ff0c52d6p-1 * r + 1;
  y = z * r2 + y;
  return (float)(y * s);
}

static const double kLog2Table[16][2] = {
    {0x1.661ec79f8f3bep+0, -0x1.efec65b963019p-2}, {0x1.571ed4aaf883dp+0, -0x1.b0b6832d4fca4p-2},
    {0x1.49539f0f010bp+0, -0x1.7418b0a1fb77bp-2}, {0x1.3c995b0b80385p+0, -0x1.39de91a6dcf7bp-2},
    {0x1.30d190c8864a5p+0, -0x1.01d9bf3f2b631p-2}, {0x1.25e227b0b8eap+0, -0x1.97c1d1b3b7afp-3},
    {0x1.1bb4a4a1a343fp+0, -0x1.2f9e393af3c9fp-3}, {0x1.12358f08ae5bap+0, -0x1.960cbbf788d5cp-4},
    {0x1.0953f419900a7p+0, -0x1.a6f9db6475fcep-5}, {0x1p+0, 0x0p+0},
    {0x1.e608cfd9a47acp-1, 0x1.338ca9f24f53dp-4}, {0x1.ca4b31f026aap-1, 0x1.476a9543891bap-3},
    {0x1.b2036576afce6p-1, 0x1.e840b4ac4e4d2p-3}, {0x1.9c2d163a1aa2dp-1, 0x1.40645f0c6651cp-2},
    {0x1.886e6037841edp-1, 0x1.88e9c2c1b9ff8p-2}, {0x1.767dcf5534862p-1, 0x1.ce0a44eb17bccp-2}};

static float Unfused_log2f(float x) {
  uint32_t ix = Bits(x);
  if (ix == 0x3f800000 || ix - 0x00800000 >= 0x7f800000 - 0x00800000) return log2f(x); /* not compared */
  uint32_t tmp = ix - 0x3f330000, top = tmp & 0xff800000, iz = ix - top;
  int i = (tmp >> 19) % 16, k = (int32_t)tmp >> 23;
  float zf;
  memcpy(&zf, &iz, 4);
  double z = zf, r = z * kLog2Table[i][0] - 1, y0 = kLog2Table[i][1] + (double)k, r2 = r * r;
  double y = 0x1.ecabf496832ep-2 * r + -0x1.715479ffae3dep-1;
  y = -0x1.712b6f70a7e4dp-2 * r2 + y;
  double p = 0x1.715475f35c8b8p0 * r + y0;
  y = y * r2 + p;
  return (float)y;
}

static uint64_t Bits64(double d) {
  uint64_t u;
  memcpy(&u, &d, 8);
  return u;
}

static double FromBits64(uint64_t u) {
  double d;
  memcpy(&d, &u, 8);
  return d;
}

static uint64_t Mix64(uint64_t hash, double result) {
  uint64_t bits = isnan(result) ? 0x7ff8000000000000ull : Bits64(result);
  for (int i = 0; i < 8; i++) {
    hash ^= (bits >> (8 * i)) & 0xff;
    hash *= 0x100000001b3ull;
  }
  return hash;
}

// mulberry32, as the composer and MathArguments draw it.
static uint32_t state;
static uint32_t Next(void) {
  state += 0x6D2B79F5u;
  uint32_t t = state;
  t = (t ^ (t >> 15)) * (t | 1);
  t ^= t + (t ^ (t >> 7)) * (t | 61);
  return t ^ (t >> 14);
}
static double Uniform(void) { return Next() / 4294967296.0; }
static double RandomBits(void) {
  uint64_t hi = Next();
  uint64_t lo = Next();
  return FromBits64(hi << 32 | lo);
}

static const double kSpecialX[] = {0.0, -0.0, INFINITY, -INFINITY, NAN, 1, -1, 2, -2, 0.5,
                                   4.9406564584124654e-324, -4.9406564584124654e-324, 2.2250738585072014e-308,
                                   1.7976931348623157e308, -1.7976931348623157e308,
                                   0x1.fffffffffffffp-1, 0x1.0000000000001p0, 10, -10, 3};
static const double kSpecialY[] = {0.0, -0.0, INFINITY, -INFINITY, NAN, 0.5, -0.5, 1, -1, 2, -2, 3, -3,
                                   0x1p-70, -0x1p-70, 0x1p70, -0x1p70, 1075, -1075, 0.35, 1e-300};

static double Special(const double *specials, uint32_t count) {
  uint32_t index = Next() % (count + 4);
  return index < count ? specials[index] : RandomBits();
}

static void PowFamily(const char *name, int family, int last) {
  state = (uint32_t)(family + 1) * 7919u;
  uint64_t hash = 0xcbf29ce484222325ull;
  printf("\"%s\":{\"first\":[", name);
  for (int i = 0; i < (1 << 22); i++) {
    double x, y;
    if (family == 0) {
      uint64_t hi = Next();
      uint64_t lo = Next();
      x = FromBits64((hi << 32 | lo) & 0x7fefffffffffffffull);
      y = (Uniform() * 2 - 1) * 8;
    } else if (family == 1) {
      x = 0.5 + Uniform() * 1.5;
      y = (Uniform() * 2 - 1) * 2048;
    } else if (family == 2) {
      x = -(1e-3 + Uniform() * 1e3);
      double coin = Uniform();
      double u = Uniform();
      y = coin < 0.5 ? floor(u * 81) - 40 : (u * 2 - 1) * 40;
    } else if (family == 3) {
      x = Special(kSpecialX, sizeof kSpecialX / sizeof *kSpecialX);
      y = Special(kSpecialY, sizeof kSpecialY / sizeof *kSpecialY);
    } else {
      int m = (int)(Next() % 700) - 300;
      if (i % 3 == 0) {
        x = 2;
        y = (m - 69) / 12.0;
      } else if (i % 3 == 1) {
        x = (20 + Uniform() * 20000) / 330;
        y = 0.35;
      } else {
        double d = 0.3 + Uniform() * 4.7;
        double a = 20 + Uniform() * 20000;
        x = 0.001;
        y = 1 / (d * a);
      }
    }
    double r = pow(x, y);
    hash = Mix64(hash, r);
    if (i < 8) printf("%s\"%016llx\"", i ? "," : "", (unsigned long long)(isnan(r) ? 0x7ff8000000000000ull : Bits64(r)));
  }
  printf("],\"hash\":\"%016llx\"}%s", (unsigned long long)hash, last ? "" : ",");
}

int main(void) {
  long exp2_unfused_differs = 0, log2_unfused_differs = 0;
  const uint64_t kOffset = 0xcbf29ce484222325ull;
  uint64_t log10_all = kOffset, log10_sample = kOffset, pow10_all = kOffset, pow10_sample = kOffset;
  uint64_t exp2_all = kOffset, log2_all = kOffset, exp2_sample = kOffset, log2_sample = kOffset;
  for (uint64_t b = 0; b < 0x100000000ull; b++) {
    float x;
    uint32_t u = (uint32_t)b;
    memcpy(&x, &u, 4);
    float e = exp2f(x), l = log2f(x);
    exp2_all = Mix(exp2_all, e);
    log2_all = Mix(log2_all, l);
    if (Bits(e) != Bits(Unfused_exp2f(x)) && !isnan(e)) exp2_unfused_differs++;
    if (Bits(l) != Bits(Unfused_log2f(x)) && !isnan(l)) log2_unfused_differs++;
    float l10 = log10f(x), p10 = powf(10.0f, x);
    log10_all = Mix(log10_all, l10);
    pow10_all = Mix(pow10_all, p10);
    if (b % 4099 == 0) {
      log10_sample = Mix(log10_sample, l10);
      pow10_sample = Mix(pow10_sample, p10);
      exp2_sample = Mix(exp2_sample, e);
      log2_sample = Mix(log2_sample, l);
    }
  }
  printf("{\"glibc\":\"%s\",\"sampleStride\":4099,"
         "\"exp2f\":{\"all\":\"%016llx\",\"sample\":\"%016llx\",\"unfusedDiffers\":%ld},"
         "\"log2f\":{\"all\":\"%016llx\",\"sample\":\"%016llx\",\"unfusedDiffers\":%ld},"
         "\"log10f\":{\"all\":\"%016llx\",\"sample\":\"%016llx\",\"unfusedDiffers\":0},"
         "\"pow10f\":{\"all\":\"%016llx\",\"sample\":\"%016llx\",\"unfusedDiffers\":0},",
         GLIBC_VERSION_STRING, (unsigned long long)exp2_all, (unsigned long long)exp2_sample, exp2_unfused_differs,
         (unsigned long long)log2_all, (unsigned long long)log2_sample, log2_unfused_differs,
         (unsigned long long)log10_all, (unsigned long long)log10_sample,
         (unsigned long long)pow10_all, (unsigned long long)pow10_sample);
  printf("\"pow\":{");
  const char *families[] = {"random", "nearOne", "negative", "special", "composer"};
  for (int f = 0; f < 5; f++) PowFamily(families[f], f, f == 4);
  printf("}}\n");
}
