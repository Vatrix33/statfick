#version 460
// Glow, pass 4 (Docs/Renderer-3D.md, "Glow"): the lizardman's halo, as compose.py made it in 8-bit
// sRGB: the wide blur with its alpha boosted (its colours kept), the tight blur over it, laid over
// the view behind the whole model it belongs to: a sample of that model, or of anything nearer than
// its glowing parts, hides it.
#include "common.glsl"

layout(set = 0, binding = 0) uniform utexture2D UintTextures[];
layout(set = 0, binding = 0) uniform utexture2DMS SurfaceSamples[];
layout(push_constant, scalar) uniform Push
{
    uint scene;
    uint wide;
    uint core;
    uint owner;
    uint surfaces;
    float boost;
} pc;

layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

float encode(float c) { return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055; }
float decode(float c) { return c <= 0.04045 ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4); }

vec4 fetch(uint index, ivec2 p)
{
    return texelFetch(sampler2D(Textures[nonuniformEXT(index)], Samplers[SAMPLER_NEAREST_CLAMP]), p, 0);
}

void main()
{
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec4 scene = fetch(pc.scene, p);
    vec3 under = vec3(encode(scene.r), encode(scene.g), encode(scene.b));
    vec4 wide = fetch(pc.wide, p);
    vec4 core = fetch(pc.core, p);
    // Straight colours: compose.py's "RGBa" to "RGBA", then the wide halo's alpha times the boost.
    vec3 wideColor = wide.a > 0.0 ? wide.rgb / wide.a : vec3(0.0);
    float wideAlpha = min(1.0, wide.a * pc.boost);
    vec3 coreColor = core.a > 0.0 ? core.rgb / core.a : vec3(0.0);
    float alpha = core.a + wideAlpha * (1.0 - core.a);
    vec3 halo = coreColor * core.a + wideColor * wideAlpha * (1.0 - core.a);
    // Behind its model: the share of this pixel's samples that are neither the model nor nearer.
    uvec2 owner = texelFetch(usampler2D(UintTextures[nonuniformEXT(pc.owner)], Samplers[SAMPLER_NEAREST_CLAMP]), p, 0).xy;
    float open = 0.0;
    for (int i = 0; i < 4; i++)
    {
        uvec2 s = texelFetch(usampler2DMS(SurfaceSamples[nonuniformEXT(pc.surfaces)], Samplers[SAMPLER_NEAREST_CLAMP]), p, i).xy;
        uint model = s.x & 1023u;
        bool behind = model == 0u || model != owner.x && uintBitsToFloat(s.y) > uintBitsToFloat(owner.y);
        open += owner.x != 0u && behind ? 0.25 : 0.0;
    }
    vec3 result = halo * open + under * (1.0 - alpha * open);
    outColor = vec4(decode(result.r), decode(result.g), decode(result.b), scene.a);
}
