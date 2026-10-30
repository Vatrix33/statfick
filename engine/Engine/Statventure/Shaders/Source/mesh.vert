#version 460
// 3D meshes: model and camera matrices (uploaded as System.Numerics row-vector matrices, which
// GLSL reads transposed, so M * v here is v * M there).
#include "common.glsl"
#include "mesh_common.glsl"

layout(location = 0) out vec3 vNormal;

void main()
{
    MeshVertex v = pc.vertices.items[gl_VertexIndex];
    vec4 world = pc.draw.model * vec4(v.position, 1.0);
    vNormal = mat3(pc.draw.model) * v.normal;
    gl_Position = pc.camera.viewProjection * world;
}
