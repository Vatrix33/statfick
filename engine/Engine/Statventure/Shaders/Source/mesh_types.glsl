// 3D meshes (M6): the types of vertices, a draw's data and the scene's, pulled from buffers by address.
struct MeshVertex { vec3 position; vec3 normal; vec2 uv; };
layout(buffer_reference, scalar) readonly buffer MeshVertices { MeshVertex items[]; };
// model: object to world; normalMatrix: its inverse transpose, for normals; color and shadow: the lit
// and shadow tones (linear); surface: the draw's model (bits 0-9), part (10-17) and ink (18) for
// the ink pass (Docs/Renderer-3D.md, "Ink lines"); receivesShadows: 1 for scenery; anchor: where its
// model is (world), which the hatching moves with; hatch: 1 when hatched in shadow.
// A terrain's cover blending (terrain.frag): where its grid starts (world x, z) and a cell's size,
// the bindless index of its cover map (each grid point's cover), the grid's size, the number of
// covers, and the covers' tones (lit, then shadow, linear).
layout(buffer_reference, scalar) readonly buffer TerrainShade
{
    vec2 origin; float cellSize; uint covers; uvec2 grid; uint coverCount; uint pad0; vec4 tones[];
};
// extra: a terrain's cover blending (TerrainShade), else 0. islands: each vertex's island (Model's SurfaceIslands),
// else 0 (one piece); sheet: the number of the mesh drawn (its primitives share it), which with the island makes each
// sample's sheet (sheetOf) for the ink's feature edges (Docs/Renderer-3D.md, "Feature edges").
layout(buffer_reference, scalar) readonly buffer IslandIds { uint items[]; };
layout(buffer_reference, scalar) readonly buffer MeshDraw
{
    mat4 model; mat4 normalMatrix; vec4 color; vec4 shadow; uint surface; uint receivesShadows; vec3 anchor; uint hatch; TerrainShade extra;
    uint64_t islands; uint sheet;
};

// A sheet: a piece of one drawn mesh (its number and the island), hashed to the 16 bits the sheet records keep.
uint sheetOf(uint mesh, uint island)
{
    uint h = mesh * 0x9E3779B1u ^ (island + 0x7F4A7C15u) * 0x85EBCA77u;
    h ^= h >> 15;
    h *= 0x2C1B3C6Du;
    h ^= h >> 12;
    return h & 0xFFFFu;
}
// sun.xyz: the direction toward the light; sun.w: the diffuse light it gives a white surface facing
// it (Blender: strength / π); band: the cel band edge on that diffuse light; orthographic: 1 when
// the camera is. The shadow map (Docs/Renderer-3D.md, "Shadows"): lightViewProjection, world to
// the view from the sun's centre; shadowScale, its units per world unit across, up and toward the
// light, and w the tangent of the sun's half angle; shadowMap, the bindless index of the atlas of
// 16 maps (~0u: none); shadowTexel, a texel in world units; shadowFar, the light-view distance at
// depth 0. ink: the ink colour (linear); viewport: the target's size in pixels; the hatching
// (Docs/Renderer-3D.md, "Hatching"): its lines' normal on screen (Y down), spacing and width in
// pixels, and how far toward the ink a line takes the shadow; time: the scene's clock in seconds
// (the water's waves).
layout(buffer_reference, scalar) readonly buffer MeshScene
{
    mat4 viewProjection; mat4 view; vec4 sun; float band; uint orthographic;
    mat4 lightViewProjection; vec4 shadowScale; uint shadowMap; float shadowTexel; float shadowFar;
    vec4 ink; vec2 viewport; vec2 hatchNormal; float hatchSpacing; float hatchWidth; float hatchInk; float time;
};

