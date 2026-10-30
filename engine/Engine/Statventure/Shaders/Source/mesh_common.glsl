// 3D meshes (M6): vertices, the draw's data and the scene's, pulled from buffers by address.
struct MeshVertex { vec3 position; vec3 normal; vec2 uv; };
layout(buffer_reference, scalar) readonly buffer MeshVertices { MeshVertex items[]; };
// model: object to world; normalMatrix: its inverse transpose, for normals; color and shadow: the lit
// and shadow tones (linear).
layout(buffer_reference, scalar) readonly buffer MeshDraw { mat4 model; mat4 normalMatrix; vec4 color; vec4 shadow; };
// sun.xyz: the direction toward the light; sun.w: the diffuse light it gives a white surface facing
// it (Blender: strength / π); band: the cel band edge on that diffuse light.
layout(buffer_reference, scalar) readonly buffer MeshScene { mat4 viewProjection; vec4 sun; float band; };
layout(push_constant, scalar) uniform Push { MeshVertices vertices; MeshDraw draw; MeshScene scene; } pc;
