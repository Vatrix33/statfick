#version 460
// Canvas filter passes over premultiplied textures (layers):
//   mode 0: copy, mode 1: colour matrix, mode 2: drop shadow (blurred alpha in shadowTexture,
//   tinted and offset) under the source.
#include "common.glsl"

layout(push_constant, scalar) uniform Push
{
    vec4 sourceRect;      // uv region of the source
    mat4 matrix;          // colour matrix (mode 1), applied to straight colour
    vec4 offsetOrColor;   // mode 1: matrix offset; mode 2: shadow colour (premultiplied)
    vec2 shadowShift;     // mode 2: uv offset of the shadow
    uint textureIndex;
    uint shadowTexture;
    uint mode;
} pc;

layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

void main()
{
    vec2 uv = mix(pc.sourceRect.xy, pc.sourceRect.zw, vUv);
    vec4 source = sampleTexture(pc.textureIndex, SAMPLER_LINEAR_CLAMP, uv);
    if (pc.mode == 1u)
    {
        vec4 straight = source.a > 0.0 ? vec4(source.rgb / source.a, source.a) : vec4(0.0);
        vec4 result = clamp(pc.matrix * straight + pc.offsetOrColor, 0.0, 1.0);
        outColor = vec4(result.rgb * result.a, result.a);
    }
    else if (pc.mode == 2u)
    {
        vec2 shadowUv = uv - pc.shadowShift;
        bool inside = all(greaterThanEqual(shadowUv, pc.sourceRect.xy)) && all(lessThanEqual(shadowUv, pc.sourceRect.zw));
        float alpha = inside ? sampleTexture(pc.shadowTexture, SAMPLER_LINEAR_CLAMP, shadowUv).a : 0.0;
        vec4 shadow = pc.offsetOrColor * alpha;
        outColor = source + shadow * (1.0 - source.a);
    }
    else
        outColor = source;
}
