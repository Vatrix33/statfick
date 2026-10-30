#version 460
// Engine test: samples a bindless texture over the whole target, at a chosen mip level.
#include "common.glsl"

layout(push_constant, scalar) uniform Push { uint textureIndex; uint samplerIndex; float lod; } pc;
layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

void main()
{
    outColor = pc.lod < 0.0 ? sampleTexture(pc.textureIndex, pc.samplerIndex, vUv) : sampleTextureLod(pc.textureIndex, pc.samplerIndex, vUv, pc.lod);
}
