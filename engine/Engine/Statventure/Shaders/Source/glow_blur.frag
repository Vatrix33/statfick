#version 460
// Glow, pass 2 (Docs/Renderer-3D.md, "Glow"): one pass of Pillow's GaussianBlur, which the
// lizardman's compose.py used: an extended box (2·radius + 1 pixels at full weight, one more at each
// end at the fractional weight), three passes along each axis, the image's edge pixels repeated.
// Premultiplied colour, as compose.py blurred "RGBa".
#include "common.glsl"

layout(push_constant, scalar) uniform Push
{
    uint source;
    int radius;
    float fraction;
    int horizontal;
    vec2 size;
} pc;

layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

void main()
{
    ivec2 p = ivec2(gl_FragCoord.xy);
    ivec2 step = pc.horizontal != 0 ? ivec2(1, 0) : ivec2(0, 1);
    ivec2 last = ivec2(pc.size) - 1;
    vec4 sum = vec4(0.0);
    for (int k = -pc.radius - 1; k <= pc.radius + 1; k++)
    {
        vec4 value = texelFetch(sampler2D(Textures[nonuniformEXT(pc.source)], Samplers[SAMPLER_NEAREST_CLAMP]), clamp(p + step * k, ivec2(0), last), 0);
        sum += value * (abs(k) == pc.radius + 1 ? pc.fraction : 1.0);
    }
    outColor = sum / (2.0 * (float(pc.radius) + pc.fraction) + 1.0);
}
