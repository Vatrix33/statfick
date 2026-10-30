#version 460
// Ink lines, pass 1 (Docs/Renderer-3D.md, "Ink lines"): which pixel boundaries carry a line, from
// the surface records of the 3D pass (mesh_surface.glsl), and where on them. Sample 0 of a pixel
// and of its right (lower) neighbour decide the line:
// - The nearer side owns a boundary; a part drawn without ink has no lines.
// - Another model (or the background) on the other side: the owner's outline.
// - Within a model, the surface breaking off (neither side's plane runs on to the other's depth):
//   a silhouette, an inner line.
// - Two parts of a model meeting without a break: no line (Freestyle draws no intersections).
// - Within a part, face normals further apart than the crease angle allows: a crease, an inner line.
// All eight samples of the two pixels place it: a straight edge crossing the two-pixel strip leaves
// as much of the strip on the first pixel's side as the distance at which it crosses the strip's
// middle, and the samples on that side count that in quarters.
// A mesh's feature edges (feature.frag: creases and open edges, as Freestyle finds them on the mesh)
// add the nearest point on an edge at a pixel, kept where the edge lies on the visible surface
// (within two pixels' worth of depth) and its part has ink: a plate's edge lying on a body, which
// the per-pixel tests above see as one continuous surface.
// Out: bits 0-1, the line to the right neighbour (1 inner, 2 outline); bits 2-4, the samples of
// the two pixels on this pixel's side, less one (the crossing at (count) / 4 from this pixel's
// left); bits 5-9 the same toward the pixel below (crossing from its top); bit 10, a feature edge
// point, its offset from the pixel's centre in bits 11-15 and 16-20 (−0.75..0.75 in 31 steps).
#include "common.glsl"

layout(set = 0, binding = 0) uniform utexture2DMS SurfaceSamples[];
layout(set = 0, binding = 0) uniform utexture2D UintTextures[];
layout(push_constant, scalar) uniform Push
{
    uint surfaces;
    uint orthographic;
    vec2 size;
    // Perspective: tan of half the field of view across and up; orthographic: half the view's width and height.
    vec2 halfExtent;
    float creaseCos;
    uint features;
} pc;

layout(location = 0) in vec2 vUv;
layout(location = 0) out uint outEdges;

struct Surface { uint model; uint part; bool ink; vec3 normal; float depth; };

Surface load(ivec2 p, int sampleIndex)
{
    uvec2 r = texelFetch(usampler2DMS(SurfaceSamples[nonuniformEXT(pc.surfaces)], Samplers[SAMPLER_NEAREST_CLAMP]), p, sampleIndex).xy;
    Surface s;
    s.model = r.x & 1023u;
    s.part = (r.x >> 10) & 255u;
    s.ink = ((r.x >> 18) & 1u) != 0u;
    vec2 e = vec2((r.x >> 19) & 63u, (r.x >> 25) & 63u) / 63.0 * 2.0 - 1.0;
    vec3 n = vec3(e, 1.0 - abs(e.x) - abs(e.y));
    if (n.z < 0.0)
        n.xy = (1.0 - abs(n.yx)) * vec2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    s.normal = normalize(n);
    s.depth = uintBitsToFloat(r.y);
    return s;
}

// The view ray through a point on screen (pixels): origin and direction (z −1, so the distance along it is the depth).
void rayAt(vec2 point, out vec3 origin, out vec3 direction)
{
    vec2 ndc = point / pc.size * 2.0 - 1.0;
    vec2 v = vec2(ndc.x, -ndc.y) * pc.halfExtent;
    origin = pc.orthographic != 0u ? vec3(v, 0.0) : vec3(0.0);
    direction = pc.orthographic != 0u ? vec3(0.0, 0.0, -1.0) : vec3(v, -1.0);
}

// The view ray through a pixel's centre.
void ray(ivec2 p, out vec3 origin, out vec3 direction)
{
    rayAt(vec2(p) + 0.5, origin, direction);
}

// How wide a pixel is at a depth.
float pixelSize(float depth)
{
    return 2.0 * pc.halfExtent.y / pc.size.y * (pc.orthographic != 0u ? 1.0 : depth);
}

// The depth at which pixel b's ray meets a's face plane.
float planeDepth(Surface a, ivec2 pa, ivec2 pb)
{
    vec3 originA, directionA, originB, directionB;
    ray(pa, originA, directionA);
    ray(pb, originB, directionB);
    float across = dot(a.normal, directionB);
    if (abs(across) < 1e-6)
        return 1e30;
    return dot(a.normal, originA + directionA * a.depth - originB) / across;
}

// The depth surface a (at pa) leads to at the next pixel pb: on from the pixel behind it when that
// pixel is the same smooth surface (depth for orthographic cameras and its reciprocal for
// perspective ones run on linearly across the screen, exactly on a plane), else along its face
// plane. Following the surface over two pixels keeps a curved surface seen at a grazing angle
// continuous, where a face plane nearly along the ray would miss by far.
float predictedDepth(Surface a, ivec2 pa, ivec2 pb)
{
    ivec2 behind = 2 * pa - pb;
    if (all(greaterThanEqual(behind, ivec2(0))) && all(lessThan(behind, ivec2(pc.size))))
    {
        Surface t = load(behind, 0);
        if (t.model == a.model && t.part == a.part && dot(t.normal, a.normal) > 0.9)
        {
            if (pc.orthographic != 0u)
                return 2.0 * a.depth - t.depth;
            float reciprocal = 2.0 / a.depth - 1.0 / t.depth;
            return reciprocal > 0.0 ? 1.0 / reciprocal : 1e30;
        }
    }
    return planeDepth(a, pa, pb);
}

// How squarely a surface faces the viewer at pixel p (1 facing, 0 edge-on).
float facing(Surface a, ivec2 p)
{
    vec3 origin, direction;
    ray(p, origin, direction);
    return abs(dot(a.normal, normalize(direction)));
}

// The surface breaks between a and b: neither runs on to the other's depth, within two pixels'
// worth of depth at the nearer one. Within a part, faces less than the crease angle apart with
// one seen almost edge-on go on: there a face's plane runs nearly along the view ray and predicts
// nothing (the pixel behind it is often another surface).
bool broken(Surface a, ivec2 pa, Surface b, ivec2 pb)
{
    float depth = min(a.depth, b.depth);
    float tolerance = 2.0 * pixelSize(depth) + 1e-4 * depth;
    if (abs(predictedDepth(a, pa, pb) - b.depth) <= tolerance || abs(predictedDepth(b, pb, pa) - a.depth) <= tolerance)
        return false;
    return !(a.part == b.part && dot(a.normal, b.normal) >= pc.creaseCos && min(facing(a, pa), facing(b, pb)) < 0.25);
}

// 0: no line, 1: an inner line, 2: an outline.
uint classify(Surface a, ivec2 pa, Surface b, ivec2 pb)
{
    if (a.model == 0u && b.model == 0u)
        return 0u;
    bool aFront = b.model == 0u || (a.model != 0u && a.depth <= b.depth);
    if (!(aFront ? a.ink : b.ink))
        return 0u;
    if (a.model != b.model)
        return 2u;
    if (broken(a, pa, b, pb))
        return 1u;
    if (a.part != b.part)
        return 0u;
    return dot(a.normal, b.normal) < pc.creaseCos ? 1u : 0u;
}

// How unlike two surface records are: another model, another part, the faces' angle, the depths.
float unlike(Surface r, Surface x, float pixel)
{
    return (r.model != x.model ? 100.0 : 0.0) + (r.part != x.part ? 10.0 : 0.0) + (1.0 - dot(r.normal, x.normal))
        + min(1.0, abs(r.depth - x.depth) / (4.0 * pixel));
}

// The line between pixel p and its neighbour n, with the count of their samples on p's side.
uint edge(ivec2 p, Surface a, ivec2 n)
{
    Surface b = load(n, 0);
    uint kind = classify(a, p, b, n);
    if (kind == 0u)
        return 0u;
    float pixel = pixelSize(max(min(a.depth, b.depth), 1e-6));
    uint count = 0u;
    for (int i = 0; i < 4; i++)
    {
        Surface r = load(p, i);
        count += unlike(r, a, pixel) <= unlike(r, b, pixel) ? 1u : 0u;
        r = load(n, i);
        count += unlike(r, a, pixel) <= unlike(r, b, pixel) ? 1u : 0u;
    }
    return kind | (uint(clamp(int(count) - 1, 0, 6)) << 2);
}

void main()
{
    ivec2 p = ivec2(gl_FragCoord.xy);
    ivec2 size = ivec2(pc.size);
    Surface s = load(p, 0);
    uint right = p.x + 1 < size.x ? edge(p, s, p + ivec2(1, 0)) : 0u;
    uint down = p.y + 1 < size.y ? edge(p, s, p + ivec2(0, 1)) : 0u;
    uint result = right | (down << 5);
    uvec4 feature = texelFetch(usampler2D(UintTextures[nonuniformEXT(pc.features)], Samplers[SAMPLER_NEAREST_CLAMP]), p, 0);
    if (feature.w != 0u && ((feature.x >> 18) & 1u) != 0u)
    {
        // The edge point is seen when it isn't behind the visible surface: that surface's plane, carried
        // to the point, within half a pixel's worth of depth (Freestyle's visibility is exact). A
        // surface seen nearly edge-on predicts nothing; there, within two pixels' worth.
        float depth = uintBitsToFloat(feature.y);
        vec2 offset = (vec2(feature.z & 31u, (feature.z >> 5) & 31u) / 31.0 * 2.0 - 1.0) * 0.75;
        bool visible = s.model == 0u;
        if (!visible)
        {
            vec3 origin, direction, pointOrigin, pointDirection;
            ray(p, origin, direction);
            rayAt(vec2(p) + 0.5 + offset, pointOrigin, pointDirection);
            float across = dot(s.normal, pointDirection);
            float pixel = pixelSize(min(depth, s.depth));
            if (abs(across) > 0.25 * length(pointDirection))
            {
                float surface = dot(s.normal, origin + direction * s.depth - pointOrigin) / across;
                visible = depth <= surface + 0.5 * pixel + 1e-4 * depth;
            }
            else
                visible = depth <= s.depth + 2.0 * pixel + 1e-4 * depth;
        }
        if (visible)
            result |= 1u << 10 | (feature.z & 1023u) << 11;
    }
    outEdges = result;
}
