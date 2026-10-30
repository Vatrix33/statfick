// 3D meshes (M6): vertices, the draw's data and the camera, pulled from buffers by address.
struct MeshVertex { vec3 position; vec3 normal; vec2 uv; };
layout(buffer_reference, scalar) readonly buffer MeshVertices { MeshVertex items[]; };
layout(buffer_reference, scalar) readonly buffer MeshDraw { mat4 model; vec4 color; };
layout(buffer_reference, scalar) readonly buffer MeshCamera { mat4 viewProjection; };
layout(push_constant, scalar) uniform Push { MeshVertices vertices; MeshDraw draw; MeshCamera camera; } pc;
