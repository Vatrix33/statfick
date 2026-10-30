#version 460
// Ink lines, pass 2 (Docs/Renderer-3D.md, "Ink lines"): each line centred on the edge points
// ink_edges.frag found: where an edge crosses the middle of a pixel and its right (lower)
// neighbour. The line covers what lies within half its width of an edge point, so ends come out
// round and coverage falls off over a pixel. Drawn over the 3D view with straight alpha.
#include "common.glsl"

layout(set = 0, binding = 0) uniform utexture2D UintTextures[];
layout(push_constant, scalar) uniform Push
{
    uint edges;
    int reach;
    vec2 size;
    float outlineHalf;
    float innerHalf;
    vec4 ink;
} pc;

layout(location = 0) in vec2 vUv;
layout(location = 0) out vec4 outColor;

void main()
{
    ivec2 q = ivec2(gl_FragCoord.xy);
    ivec2 size = ivec2(pc.size);
    float outline = 1e9;
    float inner = 1e9;
    for (int dy = -pc.reach; dy <= pc.reach; dy++)
        for (int dx = -pc.reach; dx <= pc.reach; dx++)
        {
            ivec2 c = q + ivec2(dx, dy);
            if (c.x < 0 || c.y < 0 || c.x >= size.x || c.y >= size.y)
                continue;
            uint e = texelFetch(usampler2D(UintTextures[nonuniformEXT(pc.edges)], Samplers[SAMPLER_NEAREST_CLAMP]), c, 0).x;
            if (e == 0u)
                continue;
            // Pixel c's edge points, from this pixel's centre: the crossing lies (count) / 4 from
            // c's left (top), on the middle row (column) of c.
            uint right = e & 31u;
            uint down = e >> 5;
            if (right != 0u)
            {
                float d = length(vec2(dx, dy) + vec2(float((right >> 2) + 1u) / 4.0 - 0.5, 0.0));
                if ((right & 3u) == 1u) inner = min(inner, d); else outline = min(outline, d);
            }
            if (down != 0u)
            {
                float d = length(vec2(dx, dy) + vec2(0.0, float((down >> 2) + 1u) / 4.0 - 0.5));
                if ((down & 3u) == 1u) inner = min(inner, d); else outline = min(outline, d);
            }
        }
    float coverage = max(clamp(pc.outlineHalf - outline + 0.5, 0.0, 1.0), clamp(pc.innerHalf - inner + 0.5, 0.0, 1.0));
    if (coverage <= 0.0)
        discard;
    outColor = vec4(pc.ink.rgb, coverage * pc.ink.a);
}
