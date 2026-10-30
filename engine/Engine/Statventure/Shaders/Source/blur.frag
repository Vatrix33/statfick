#version 460
// One direction of a separable Gaussian blur over a premultiplied texture region.
#include "common.glsl"

layout(push_constant, scalar) uniform Push
{
    vec4 sourceRect;   // uv of the region to blur: u0 v0 u1 v1
    vec2 texelStep;    // uv step for one pixel along the blur direction
    float sigma;       // pixels
    uint textureIndex;
    uint alphaOnly;    // blur only alpha (shadows)
    uint mirrorEdges;  // 1: mirror the region at its edges (backdrop-filter); 0: transparent outside
} pc;

layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

void main()
{
    vec2 uv = mix(pc.sourceRect.xy, pc.sourceRect.zw, vUv);
    int radius = int(ceil(pc.sigma * 3.0));
    vec4 sum = vec4(0.0);
    float total = 0.0;
    for (int i = -radius; i <= radius; i++)
    {
        float w = exp(-float(i * i) / (2.0 * pc.sigma * pc.sigma));
        vec2 raw = uv + pc.texelStep * float(i);
        vec4 s;
        if (pc.mirrorEdges != 0u)
        {
            // Reflect back into the region.
            vec2 lo = pc.sourceRect.xy, hi = pc.sourceRect.zw;
            vec2 at = raw;
            at = mix(at, 2.0 * lo - at, vec2(lessThan(at, lo)));
            at = mix(at, 2.0 * hi - at, vec2(greaterThan(at, hi)));
            s = sampleTexture(pc.textureIndex, SAMPLER_LINEAR_CLAMP, clamp(at, lo, hi));
        }
        else
        {
            // Outside the region counts as transparent.
            bool inside = all(greaterThanEqual(raw, pc.sourceRect.xy)) && all(lessThanEqual(raw, pc.sourceRect.zw));
            s = inside ? sampleTexture(pc.textureIndex, SAMPLER_LINEAR_CLAMP, raw) : vec4(0.0);
        }
        sum += s * w;
        total += w;
    }
    outColor = sum / total;
    if (pc.alphaOnly != 0u)
        outColor = vec4(0.0, 0.0, 0.0, outColor.a);
}
