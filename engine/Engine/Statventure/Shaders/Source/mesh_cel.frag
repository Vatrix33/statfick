#version 460
// 3D meshes cel-shaded (Docs/Renderer-3D.md, "Cel shading"), as the reference's Blender material
// computes it: a white Diffuse BSDF under the sun (strength / π · N·L) through Shader to RGB into a
// constant colour ramp, the shadow tone below the band edge and the lit tone from it. Back faces
// (double-sided surfaces) shade with the normal flipped, as Blender does. The band edge is
// antialiased: the lit share of the pixel is how far the light is past the edge over how much it
// changes across the pixel (fwidth), as Blender's filtered samples smooth it. Also writes the
// surface record for the ink pass. Scenery takes the sun's shadow: Blender thresholds each of its
// samples, which sees a hard shadow from one point of the sun, and averages them, so the tone the
// light gives blends toward the shadow tone by the share of the sun hidden (sunVisibility).
#include "common.glsl"
#include "mesh_common.glsl"
#include "mesh_surface.glsl"

layout(location = 0) centroid in vec3 vNormal;
layout(location = 1) in vec3 vView;
layout(location = 2) in vec3 vWorld;
layout(location = 0) out vec4 outColor;
layout(location = 1) out uvec2 outSurface;

// How much of the sun a point sees (Docs/Renderer-3D.md, "Shadows"): the mean, over 16 directions
// spread over the sun's disc, of whether the shadow map from that direction holds anything nearer
// the light, as Blender renders a sized sun by jittering its shadow over the disc. The maps share
// the centre's view sheared by distance, so a point's place in map i is its place in the centre's
// view moved by the direction's offset times its distance. Each lookup compares the four texels
// about the point and blends them bilinearly; the point is pushed out along its normal by a texel
// and a half so a surface doesn't shadow itself.
const int SHADOW_TILES = 4;
const float SHADOW_SIZE = 1024.0;

float shadowTexel(ivec2 texel, ivec2 origin)
{
    texel = clamp(texel, ivec2(0), ivec2(SHADOW_SIZE - 1.0));
    return texelFetch(sampler2D(Textures[nonuniformEXT(pc.scene.shadowMap)], Samplers[SAMPLER_NEAREST_CLAMP]), origin + texel, 0).r;
}

float sunVisibility(vec3 world, vec3 normal)
{
    vec4 light = pc.scene.lightViewProjection * vec4(world + normal * pc.scene.shadowTexel * 1.5, 1.0);
    vec2 uv = light.xy * 0.5 + 0.5;
    float depth = light.z;
    vec4 scale = pc.scene.shadowScale;
    float distance = pc.scene.shadowFar - depth / scale.z;
    float lit = 0.0;
    for (int i = 0; i < 16; i++)
    {
        float r = sqrt((float(i) + 0.5) / 16.0);
        float a = float(i) * 2.39996323;
        vec2 at = (uv + r * vec2(cos(a), sin(a)) * scale.w * distance * scale.xy) * SHADOW_SIZE - 0.5;
        if (any(lessThan(at, vec2(-0.5))) || any(greaterThan(at, vec2(SHADOW_SIZE - 0.5))))
        {
            lit += 1.0;
            continue;
        }
        ivec2 origin = ivec2(i % SHADOW_TILES, i / SHADOW_TILES) * int(SHADOW_SIZE);
        ivec2 base = ivec2(floor(at));
        vec2 f = at - vec2(base);
        float bias = depth + 1e-5;
        vec4 open = vec4(
            shadowTexel(base, origin) > bias ? 0.0 : 1.0,
            shadowTexel(base + ivec2(1, 0), origin) > bias ? 0.0 : 1.0,
            shadowTexel(base + ivec2(0, 1), origin) > bias ? 0.0 : 1.0,
            shadowTexel(base + ivec2(1, 1), origin) > bias ? 0.0 : 1.0);
        lit += mix(mix(open.x, open.y, f.x), mix(open.z, open.w, f.x), f.y);
    }
    return lit / 16.0;
}

void main()
{
    vec3 n = normalize(vNormal);
    if (!gl_FrontFacing)
        n = -n;
    float diffuse = pc.scene.sun.w * max(dot(n, pc.scene.sun.xyz), 0.0);
    float width = fwidth(diffuse);
    float lit = width > 0.0 ? clamp((diffuse - pc.scene.band) / width + 0.5, 0.0, 1.0) : step(pc.scene.band, diffuse);
    if (pc.draw.receivesShadows != 0u && pc.scene.shadowMap != ~0u && lit > 0.0)
        lit *= sunVisibility(vWorld, n);
    outColor = mix(pc.draw.shadow, pc.draw.color, lit);
    outSurface = surfaceRecord(vView);
}
