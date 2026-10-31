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

#include "mesh_lighting.glsl"

void main()
{
    vec3 n = normalize(vNormal);
    if (!gl_FrontFacing)
        n = -n;
    outColor = celTone(n, pc.draw.color, pc.draw.shadow);
    outSurface = surfaceRecord(vView);
}
