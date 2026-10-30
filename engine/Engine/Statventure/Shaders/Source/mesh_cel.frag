#version 460
// 3D meshes cel-shaded (Docs/Renderer-3D.md, "Cel shading"), as the reference's Blender material
// computes it: a white Diffuse BSDF under the sun (strength / π · N·L) through Shader to RGB into a
// constant colour ramp, the shadow tone below the band edge and the lit tone from it. Back faces
// (double-sided surfaces) shade with the normal flipped, as Blender does. The band edge is
// antialiased: the lit share of the pixel is how far the light is past the edge over how much it
// changes across the pixel (fwidth), as Blender's filtered samples smooth it. Also writes the
// surface record for the ink pass.
#include "common.glsl"
#include "mesh_common.glsl"
#include "mesh_surface.glsl"

layout(location = 0) centroid in vec3 vNormal;
layout(location = 1) in vec3 vView;
layout(location = 0) out vec4 outColor;
layout(location = 1) out uvec2 outSurface;

void main()
{
    vec3 n = normalize(vNormal);
    if (!gl_FrontFacing)
        n = -n;
    float diffuse = pc.scene.sun.w * max(dot(n, pc.scene.sun.xyz), 0.0);
    float width = fwidth(diffuse);
    float lit = width > 0.0 ? clamp((diffuse - pc.scene.band) / width + 0.5, 0.0, 1.0) : step(pc.scene.band, diffuse);
    outColor = mix(pc.draw.shadow, pc.draw.color, lit);
    outSurface = surfaceRecord(vView);
}
