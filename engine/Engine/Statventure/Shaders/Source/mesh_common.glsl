// 3D meshes (M6): what a mesh draw pushes (mesh_types.glsl).
#include "mesh_types.glsl"
layout(push_constant, scalar) uniform Push { MeshVertices vertices; MeshDraw draw; MeshScene scene; } pc;
