#version 460
// Water (Docs/Renderer-3D.md, "Terrain"): a flat surface cel-shaded in its tones, its normal tilted
// by two sine wave trains drifting with the scene's clock (about 1.5 m and 0.6 m long, crossing; up
// to about 8° together), so the light band breaks into ripples. Blender's reference rebuilds the
// same normal in nodes.
#include "common.glsl"
#include "mesh_common.glsl"
#include "mesh_surface.glsl"

layout(location = 0) centroid in vec3 vNormal;
layout(location = 1) in vec3 vView;
layout(location = 2) in vec3 vWorld;
layout(location = 0) out vec4 outColor;
layout(location = 1) out uvec2 outSurface;

#include "mesh_lighting.glsl"

// A wave train: its direction (world x, z), length (m), slope (the height's greatest gradient) and speed (m/s).
vec2 slope(vec2 xz, vec2 direction, float length, float steepness, float speed)
{
    float phase = 6.28318531 / length * (dot(direction, xz) - speed * pc.scene.time);
    return steepness * cos(phase) * direction;
}

void main()
{
    vec2 xz = vWorld.xz;
    vec2 gradient = slope(xz, vec2(0.93969262, 0.34202014), 1.5, 0.08, 0.3)
        + slope(xz, vec2(-0.42261826, 0.90630779), 0.6, 0.06, 0.18);
    vec3 n = normalize(vec3(-gradient.x, 1.0, -gradient.y));
    outColor = celTone(n, pc.draw.color, pc.draw.shadow);
    outSurface = surfaceRecord(vView);
}
