#version 460
// 3D meshes in a flat colour (linear; the sRGB target encodes it).
#include "common.glsl"
#include "mesh_common.glsl"

layout(location = 0) centroid in vec3 vNormal;
layout(location = 0) out vec4 outColor;

void main()
{
    outColor = pc.draw.color;
}
