#version 460
// 3D meshes in a flat colour (linear; the sRGB target encodes it), and the surface record.
#include "common.glsl"
#include "mesh_common.glsl"
#include "mesh_surface.glsl"

layout(location = 0) centroid in vec3 vNormal;
layout(location = 1) in vec3 vView;
layout(location = 0) out vec4 outColor;
layout(location = 1) out uvec2 outSurface;

void main()
{
    outColor = pc.draw.color;
    outSurface = surfaceRecord(vView);
}
