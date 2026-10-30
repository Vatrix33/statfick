// 3D meshes (M6): vertices, the draw's data and the scene's, pulled from buffers by address.
struct MeshVertex { vec3 position; vec3 normal; vec2 uv; };
layout(buffer_reference, scalar) readonly buffer MeshVertices { MeshVertex items[]; };
// model: object to world; normalMatrix: its inverse transpose, for normals; color and shadow: the lit
// and shadow tones (linear); surface: the draw's model (bits 0-9), part (10-17) and ink (18) for
// the ink pass (Docs/Renderer-3D.md, "Ink lines").
layout(buffer_reference, scalar) readonly buffer MeshDraw { mat4 model; mat4 normalMatrix; vec4 color; vec4 shadow; uint surface; };
// sun.xyz: the direction toward the light; sun.w: the diffuse light it gives a white surface facing
// it (Blender: strength / π); band: the cel band edge on that diffuse light; orthographic: 1 when
// the camera is.
layout(buffer_reference, scalar) readonly buffer MeshScene { mat4 viewProjection; mat4 view; vec4 sun; float band; uint orthographic; };
layout(push_constant, scalar) uniform Push { MeshVertices vertices; MeshDraw draw; MeshScene scene; } pc;

