#version 460
// Ink lines, feature edges (Docs/Renderer-3D.md, "Ink lines"): each crease or open edge of a mesh
// (found at load by Model, welded across the mesh's primitives) drawn as a thin quad around it on
// screen, so feature.frag can mark the pixels it passes with the nearest point on it. Creases no
// sharper than the crease angle are dropped. Six vertices an edge, pulled by gl_VertexIndex.
#include "common.glsl"
#include "mesh_types.glsl"

struct FeatureEdge { uint a; uint b; float cosine; };
layout(buffer_reference, scalar) readonly buffer FeatureEdges { FeatureEdge items[]; };
layout(push_constant, scalar) uniform FeaturePush { MeshVertices vertices; FeatureEdges edges; MeshDraw draw; MeshScene scene; float creaseCos; } pc;

layout(location = 0) flat out vec2 vA;
layout(location = 1) flat out vec2 vB;
layout(location = 2) flat out vec2 vDepth;

void main()
{
    FeatureEdge e = pc.edges.items[gl_VertexIndex / 6];
    // Open edges (cosine −2) always; creases when their faces are further apart than the crease angle allows.
    if (e.cosine >= pc.creaseCos)
    {
        gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
        return;
    }
    vec4 worldA = pc.draw.model * vec4(pc.vertices.items[e.a].position, 1.0);
    vec4 worldB = pc.draw.model * vec4(pc.vertices.items[e.b].position, 1.0);
    vec4 a = pc.scene.viewProjection * worldA, b = pc.scene.viewProjection * worldB;
    vec2 sa = (a.xy / a.w * 0.5 + 0.5) * pc.scene.viewport, sb = (b.xy / b.w * 0.5 + 0.5) * pc.scene.viewport;
    vA = sa;
    vB = sb;
    vDepth = vec2(-(pc.scene.view * worldA).z, -(pc.scene.view * worldB).z);
    vec2 along = sb - sa;
    float length2 = dot(along, along);
    vec2 dir = length2 > 1e-8 ? along * inversesqrt(length2) : vec2(1.0, 0.0);
    vec2 side = vec2(-dir.y, dir.x);
    // The quad reaches a pixel and a half to each side and past each end.
    int corner = gl_VertexIndex % 6;
    bool first = corner == 0 || corner == 1 || corner == 3;
    float across = corner == 0 || corner == 2 || corner == 4 ? -1.5 : 1.5;
    vec2 pixel = (first ? sa - dir * 1.5 : sb + dir * 1.5) + side * across;
    vec4 clip = first ? a : b;
    gl_Position = vec4((pixel / pc.scene.viewport * 2.0 - 1.0) * clip.w, clip.z, clip.w);
}
