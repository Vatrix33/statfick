#version 460
// Terrain (Docs/Renderer-3D.md, "Terrain"): cel-shaded like any scenery, its tones blended from the
// covers of the grid points about it: the point is warped by a fixed sum of sines (so boundaries
// wander off the grid; Blender's reference rebuilds the same in nodes), then the 4×4 grid points
// about it weigh in by a separable tent of radius 1.5 cells (a soft edge three cells wide), each
// with its cover's tones.
#include "common.glsl"
#include "mesh_common.glsl"
#include "mesh_surface.glsl"

layout(set = 0, binding = 0) uniform utexture2D UintTextures[];
layout(location = 0) centroid in vec3 vNormal;
layout(location = 1) in vec3 vView;
layout(location = 2) in vec3 vWorld;
layout(location = 0) out vec4 outColor;
layout(location = 1) out uvec2 outSurface;

#include "mesh_lighting.glsl"

vec2 warp(vec2 g)
{
    return 0.45 * vec2(sin(0.93 * g.y + 1.3) + 0.5 * sin(0.37 * (g.x + g.y) + 0.4),
                       sin(0.81 * g.x + 2.1) + 0.5 * sin(0.41 * (g.x - g.y) + 1.7));
}

void main()
{
    TerrainShade shade = pc.draw.extra;
    vec2 g = (vWorld.xz - shade.origin) / shade.cellSize;
    g += warp(g);
    ivec2 base = ivec2(floor(g));
    float weights[16];
    for (int c = 0; c < 16; c++)
        weights[c] = 0.0;
    float total = 0.0;
    for (int dy = -1; dy <= 2; dy++)
        for (int dx = -1; dx <= 2; dx++)
        {
            ivec2 p = base + ivec2(dx, dy);
            vec2 d = abs(g - vec2(p));
            float w = max(0.0, 1.0 - d.x / 1.5) * max(0.0, 1.0 - d.y / 1.5);
            if (w <= 0.0)
                continue;
            uint cover = texelFetch(usampler2D(UintTextures[nonuniformEXT(shade.covers)], Samplers[SAMPLER_NEAREST_CLAMP]),
                clamp(p, ivec2(0), ivec2(shade.grid) - 1), 0).x;
            weights[min(cover, 15u)] += w;
            total += w;
        }
    vec4 lit = vec4(0.0), shadow = vec4(0.0);
    for (uint c = 0u; c < shade.coverCount && c < 16u; c++)
    {
        lit += shade.tones[c * 2u] * weights[c];
        shadow += shade.tones[c * 2u + 1u] * weights[c];
    }
    lit /= total;
    shadow /= total;
    vec3 n = normalize(vNormal);
    outColor = celTone(n, lit, shadow);
    outSurface = surfaceRecord(vView);
}
