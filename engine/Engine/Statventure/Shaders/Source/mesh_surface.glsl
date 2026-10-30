// Fragment shaders of 3D meshes (after mesh_common.glsl).
// The surface record the ink pass reads (ink_edges.frag): x = the draw's surface bits, the face
// normal turned toward the viewer in view space (octahedral, 6 bits a coordinate, bits 19-24 and
// 25-30); y = the view depth (distance in front of the camera) as float bits. The face normal comes
// from the screen derivatives of the view position: within a pixel quad they're the triangle's own.
uvec2 surfaceRecord(vec3 viewPosition)
{
    vec3 n = normalize(cross(dFdx(viewPosition), dFdy(viewPosition)));
    vec3 towardViewer = pc.scene.orthographic != 0u ? vec3(0.0, 0.0, 1.0) : -viewPosition;
    if (dot(n, towardViewer) < 0.0)
        n = -n;
    n /= abs(n.x) + abs(n.y) + abs(n.z);
    vec2 e = n.z >= 0.0 ? n.xy : (1.0 - abs(n.yx)) * vec2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    uvec2 q = uvec2(round(clamp(e * 0.5 + 0.5, 0.0, 1.0) * 63.0));
    return uvec2(pc.draw.surface | (q.x << 19) | (q.y << 25), floatBitsToUint(-viewPosition.z));
}
