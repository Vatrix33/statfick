#version 460
// 3D meshes: model and camera matrices (uploaded as System.Numerics row-vector matrices, which
// GLSL reads transposed, so M * v here is v * M there). The normal goes to world space by the
// model's inverse transpose; centroid sampling keeps it inside the triangle on MSAA edges. The
// view position feeds the ink pass's surface record.
#include "common.glsl"
#include "mesh_common.glsl"

layout(location = 0) centroid out vec3 vNormal;
layout(location = 1) out vec3 vView;

void main()
{
    MeshVertex v = pc.vertices.items[gl_VertexIndex];
    vec4 world = pc.draw.model * vec4(v.position, 1.0);
    vNormal = mat3(pc.draw.normalMatrix) * v.normal;
    vView = (pc.scene.view * world).xyz;
    gl_Position = pc.scene.viewProjection * world;
}
