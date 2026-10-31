#version 460
// 3D views in the UI (Docs/Renderer-3D.md, "3D views in the UI"): a view's resolved picture
// (premultiplied linear light, read through its sRGB view) as the canvas takes images: straight
// colours, sRGB-encoded, in 8-bit unsigned normalised. An opaque view's pixels come out as they are.
#include "common.glsl"

layout(push_constant, scalar) uniform Push { uint picture; } pc;
layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

float encode(float c)
{
    return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

void main()
{
    vec4 c = texelFetch(sampler2D(Textures[nonuniformEXT(pc.picture)], Samplers[SAMPLER_NEAREST_CLAMP]), ivec2(gl_FragCoord.xy), 0);
    vec3 straight = c.a > 0.0 ? clamp(c.rgb / c.a, 0.0, 1.0) : vec3(0.0);
    outColor = vec4(encode(straight.r), encode(straight.g), encode(straight.b), c.a);
}
