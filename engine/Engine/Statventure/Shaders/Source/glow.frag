#version 460
// Glow, pass 1 (Docs/Renderer-3D.md, "Glow"): the glowing parts alone in their glow colours, as the
// lizardman's glow render had them, sRGB-encoded (its halo was made from 8-bit sRGB pixels), and
// the model each pixel belongs to with its view depth, for keeping the halo behind its model. The
// draw's glow colour (linear) comes in its shadow tone's place.
#include "common.glsl"
#include "mesh_common.glsl"

layout(location = 0) centroid in vec3 vNormal;
layout(location = 1) in vec3 vView;
layout(location = 2) in vec3 vWorld;
layout(location = 0) out vec4 outGlow;
layout(location = 1) out uvec2 outOwner;

float encode(float c)
{
    return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

void main()
{
    vec3 glow = clamp(pc.draw.shadow.rgb, 0.0, 1.0);
    outGlow = vec4(encode(glow.r), encode(glow.g), encode(glow.b), 1.0);
    outOwner = uvec2(pc.draw.surface & 1023u, floatBitsToUint(-vView.z));
}
