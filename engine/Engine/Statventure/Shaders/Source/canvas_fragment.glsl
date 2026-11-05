// Fragment helpers shared by the canvas fragment shaders.
#include "canvas_common.glsl"

// The radii of the corner in q's quadrant (q from the box's centre; tl, tr, br, bl). A corner with
// no radius on either axis is square (0, 0).
vec2 cornerRadii(vec2 q, vec4 rx, vec4 ry)
{
    vec2 r = q.x < 0.0 ? (q.y < 0.0 ? vec2(rx.x, ry.x) : vec2(rx.w, ry.w))
                       : (q.y < 0.0 ? vec2(rx.y, ry.y) : vec2(rx.z, ry.z));
    return r.x <= 0.0 || r.y <= 0.0 ? vec2(0.0) : r;
}

// Signed distance to a box (0,0)-(size) with elliptical corners: negative inside (in the units of
// p; the clips use it in pixels).
float boxDistance(vec2 p, vec2 size, vec4 rx, vec4 ry)
{
    vec2 center = size * 0.5;
    vec2 q = p - center;
    vec2 r = cornerRadii(q, rx, ry);
    vec2 d = abs(q) - center + r;
    if (r.x <= 0.0)
        return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
    if (d.x <= 0.0 || d.y <= 0.0)
        return max(d.x - r.x, d.y - r.y);
    // Inside the corner square: distance to the ellipse, scaled back to units.
    vec2 e = d / r;
    float k = length(e);
    float m = min(r.x, r.y);
    return (k - 1.0) * m;
}

// Pixel coverage from a signed distance: the distance in pixels is distance / |gradient|.
float coverage(float distance)
{
    float w = max(length(vec2(dFdx(distance), dFdy(distance))), 1e-4);
    return clamp(0.5 - distance / w, 0.0, 1.0);
}

// Pixel coverage of a box (0,0)-(size) with elliptical corners at the local point p: a ramp one
// pixel wide across the nearest edge, the distance measured in pixels. The local coordinates'
// derivatives carry local units to pixels exactly (they map to pixels affinely; the distance's own
// derivatives go wrong in 2x2 pixel quads that straddle two edges, as at the ends of a 1 px box).
// Each edge has its own scale under a transform that scales one axis more than the other (scaleX,
// a rotation scaled afterwards, a skew), so every distance is taken to pixels before distances are
// compared: across an edge x = c local x changes by the length of its gradient in pixels, so the
// edge is (x - c) / |grad x| pixels away, and likewise for y; beyond a square corner the way to the
// corner point is carried to pixels whole (through the derivatives' inverse); in an elliptical
// corner the distance is divided by its gradient's length in pixels. (One scale for the whole box,
// sqrt(|det|) of the derivatives, spread a scaleX(0.2) bar 2 px tall over four rows.)
float boxCoverage(vec2 p, vec2 size, vec4 rx, vec4 ry)
{
    // Local units a pixel along x (column 0) and along y (column 1).
    mat2 toLocal = mat2(dFdx(p), dFdy(p));
    // How much local x and local y change a pixel across their edges: their gradients' lengths in pixels.
    vec2 across = vec2(length(vec2(toLocal[0].x, toLocal[1].x)), length(vec2(toLocal[0].y, toLocal[1].y)));
    across = max(across, vec2(1e-6));
    vec2 center = size * 0.5;
    vec2 q = p - center;
    // The quadrant's signs (never 0: a point on the centre line still belongs to one side).
    vec2 side = vec2(q.x < 0.0 ? -1.0 : 1.0, q.y < 0.0 ? -1.0 : 1.0);
    vec2 r = cornerRadii(q, rx, ry);
    vec2 d = abs(q) - center + r;
    float pixels;
    if (r.x <= 0.0 && d.x > 0.0 && d.y > 0.0)
        // Beyond a square corner: the way from the corner point, in pixels.
        pixels = length(inverse(toLocal) * (side * d));
    else if (r.x <= 0.0 || d.x <= 0.0 || d.y <= 0.0)
        // Across the nearer edge, in pixels.
        pixels = max((d.x - r.x) / across.x, (d.y - r.y) / across.y);
    else
    {
        // Inside the corner square: distance to the ellipse, scaled back to units; its gradient is
        // m (e / r) / k, along the ellipse's normal (the radius on a circle).
        vec2 e = d / r;
        float k = length(e);
        float m = min(r.x, r.y);
        vec2 gradient = side * (k > 1e-6 ? m * (e / r) / k : vec2(0.0, 1.0));
        pixels = (k - 1.0) * m / max(length(gradient * toLocal), 1e-6);
    }
    return clamp(0.5 - pixels, 0.0, 1.0);
}

vec4 gradientColor(uint index, vec2 p)
{
    Gradient g = pc.gradients.items[index];
    float t;
    if (g.kind == 0u)
    {
        vec2 axis = g.geometry.zw - g.geometry.xy;
        t = dot(p - g.geometry.xy, axis) / max(dot(axis, axis), 1e-6);
    }
    else
        t = length((p - g.geometry.xy) / max(g.geometry.zw, vec2(1e-4)));
    if (g.repeating != 0u)
    {
        float first = g.offsets[0];
        float period = g.offsets[g.count - 1u] - first;
        if (period > 1e-5)
            t = first + mod(t - first, period);
    }
    if (t <= g.offsets[0])
        return g.colors[0];
    for (uint i = 1u; i < g.count; i++)
        if (t <= g.offsets[i])
        {
            float span = g.offsets[i] - g.offsets[i - 1u];
            float f = span > 1e-6 ? (t - g.offsets[i - 1u]) / span : 1.0;
            return mix(g.colors[i - 1u], g.colors[i], f);
        }
    return g.colors[g.count - 1u];
}

vec4 applyMatrix(uint index, vec4 premultiplied)
{
    if (index == NO_INDEX)
        return premultiplied;
    ColorMatrixData cm = pc.matrices.items[index];
    vec4 straight = premultiplied.a > 0.0 ? vec4(premultiplied.rgb / premultiplied.a, premultiplied.a) : vec4(0.0);
    vec4 result = clamp(cm.m * straight + cm.offset, 0.0, 1.0);
    return vec4(result.rgb * result.a, result.a);
}

// Gaussian-blurred rounded rectangle (Evan Wallace's closed form, one radius).
float gaussian(float x, float sigma) { return exp(-(x * x) / (2.0 * sigma * sigma)) / (2.5066282746 * sigma); }
vec2 erf2(vec2 x) { vec2 s = sign(x), a = abs(x); x = 1.0 + (0.278393 + (0.230389 + 0.078108 * (a * a)) * a) * a; x *= x; return s - s / (x * x); }
float shadowX(float x, float y, float sigma, float corner, vec2 halfSize)
{
    float delta = min(halfSize.y - corner - abs(y), 0.0);
    float curved = halfSize.x - corner + sqrt(max(0.0, corner * corner - delta * delta));
    vec2 integral = 0.5 + 0.5 * erf2((x + vec2(-curved, curved)) * (0.70710678 / sigma));
    return integral.y - integral.x;
}
float blurredBox(vec2 lower, vec2 upper, vec2 point, float sigma, float corner)
{
    if (sigma < 0.05)
    {
        // No blur: a crisp rounded box.
        vec2 size = upper - lower;
        return boxCoverage(point - lower, size, vec4(corner), vec4(corner));
    }
    vec2 center = (lower + upper) * 0.5;
    vec2 halfSize = (upper - lower) * 0.5;
    corner = min(corner, min(halfSize.x, halfSize.y));
    point -= center;
    float low = point.y - halfSize.y;
    float high = point.y + halfSize.y;
    float start = clamp(-3.0 * sigma, low, high);
    float end = clamp(3.0 * sigma, low, high);
    float step = (end - start) / 4.0;
    float y = start + step * 0.5;
    float value = 0.0;
    for (int i = 0; i < 4; i++)
    {
        value += shadowX(point.x, point.y - y, sigma, corner, halfSize) * gaussian(y, sigma) * step;
        y += step;
    }
    return value;
}

// Dashes along the border, as Chromium draws them: dashes twice the width, gaps the width, spread
// so that every side starts and ends with a dash.
float dashMask(vec2 p, vec2 size, vec4 widths, float dash, float gap)
{
    float top = p.y, right = size.x - p.x, bottom = size.y - p.y, left = p.x;
    float along;
    float length;
    if (min(top, bottom) < min(left, right))
    {
        along = p.x;
        length = size.x;
    }
    else
    {
        along = p.y;
        length = size.y;
    }
    float count = max(1.0, floor((length + gap) / (dash + gap) + 0.5));
    float spacing = count > 1.0 ? (length - count * dash) / (count - 1.0) : 0.0;
    float phase = mod(along, dash + spacing);
    return phase < dash ? 1.0 : 0.0;
}

float clipCoverage(vec2 canvasPixel, vec4 clip, vec4 radii)
{
    vec2 size = clip.zw - clip.xy;
    if (size.x <= 0.0 || size.y <= 0.0)
        return 0.0;
    float d = boxDistance(canvasPixel - clip.xy, size, radii, radii);
    return clamp(0.5 - d, 0.0, 1.0);
}

