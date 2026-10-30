#version 460
// 3D meshes: model and camera matrices (uploaded as System.Numerics row-vector matrices, which
// GLSL reads transposed, so M * v here is v * M there). The normal goes to world space by the
// model's inverse transpose; centroid sampling keeps it inside the triangle on MSAA edges.
#include "common.glsl"
#include "mesh_common.glsl"

layout(location = 0) centroid out vec3 vNormal;

void main()
{
    MeshVertex v = pc.vertices.items[gl_VertexIndex];
    vec4 world = pc.draw.model * vec4(v.position, 1.0);
    vNormal = mat3(pc.draw.normalMatrix) * v.normal;
    gl_Position = pc.scene.viewProjection * world;
}
