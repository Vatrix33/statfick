#version 460
// Particles (Docs/Renderer-3D.md, "Particles"): the resolved particle layer (premultiplied, linear)
// laid over the view, ink lines included, by the pipeline's premultiplied blending.
#include "common.glsl"

layout(push_constant, scalar) uniform Push { uint layer; } pc;
layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

void main()
{
    outColor = texelFetch(sampler2D(Textures[nonuniformEXT(pc.layer)], Samplers[SAMPLER_NEAREST_CLAMP]), ivec2(gl_FragCoord.xy), 0);
}
