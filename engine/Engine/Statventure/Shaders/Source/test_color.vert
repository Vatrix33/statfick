#version 460
// Engine test: coloured vertices pulled from a buffer by address.
#include "common.glsl"

struct ColorVertex { vec2 position; vec4 color; };
layout(buffer_reference, scalar) readonly buffer ColorVertices { ColorVertex items[]; };
layout(push_constant, scalar) uniform Push { ColorVertices vertices; } pc;

layout(location = 0) out vec4 vColor;

void main()
{
    ColorVertex v = pc.vertices.items[gl_VertexIndex];
    vColor = v.color;
    gl_Position = vec4(v.position, 0.0, 1.0);
}
