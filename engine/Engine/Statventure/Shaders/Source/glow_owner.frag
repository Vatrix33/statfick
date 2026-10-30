#version 460
// Glow, pass 3 (Docs/Renderer-3D.md, "Glow"): which model's halo reaches a pixel: the nearest glowing
// pixel's model and depth within the halo's reach, along one axis per pass.
#include "common.glsl"

layout(set = 0, binding = 0) uniform utexture2D UintTextures[];
layout(push_constant, scalar) uniform Push
{
    uint source;
    int reach;
    int horizontal;
    uint pad;
    vec2 size;
} pc;

layout(location = 0) in vec2 vUv;
layout(location = 0) out uvec2 outOwner;

void main()
{
    ivec2 p = ivec2(gl_FragCoord.xy);
    ivec2 step = pc.horizontal != 0 ? ivec2(1, 0) : ivec2(0, 1);
    uvec2 best = uvec2(0u);
    for (int k = -pc.reach; k <= pc.reach; k++)
    {
        ivec2 q = p + step * k;
        if (any(lessThan(q, ivec2(0))) || any(greaterThanEqual(q, ivec2(pc.size))))
            continue;
        uvec2 o = texelFetch(usampler2D(UintTextures[nonuniformEXT(pc.source)], Samplers[SAMPLER_NEAREST_CLAMP]), q, 0).xy;
        if (o.x != 0u && (best.x == 0u || uintBitsToFloat(o.y) < uintBitsToFloat(best.y)))
            best = o;
    }
    outOwner = best;
}
