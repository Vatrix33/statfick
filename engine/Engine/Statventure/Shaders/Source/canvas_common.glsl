// The 2D canvas's data, shared by canvas.vert and canvas.frag. Must match Canvas/CanvasPrimitive.cs.
#include "common.glsl"

const uint KIND_BOX = 0u;            // rounded box: background (solid or gradient) and border
const uint KIND_SHADOW = 1u;         // outer box shadow, cut out where the box is
const uint KIND_INSET_SHADOW = 2u;   // inset box shadow, inside the box
const uint KIND_IMAGE = 3u;          // texture over the box (straight alpha), rounded
const uint KIND_COVERAGE = 4u;       // red channel = coverage (glyphs, paths), painted with the brush
const uint KIND_COMPOSITE = 5u;      // premultiplied texture (a layer), rounded, colour matrix
const uint KIND_COMPOSITE_ALPHA = 6u; // a layer's alpha as a mask for the colour (drop shadows)

const uint FLAG_DASHED = 1u;
const uint FLAG_LCD32 = 4u;          // LCD mask with Skia's mask gamma built in, coverage 0-32 a channel
const uint FLAG_SKIA_IMAGE = 8u;     // a premultiplied image sampled as Skia's legacy raster shader does (skiaBilerp):
                                     // params and border.xy map canvas pixels to texels
const uint KIND_LCD = 7u;            // LCD glyph mask (rgb coverage), drawn by canvas_lcd.frag
const uint NO_INDEX = 0xffffffffu;

struct Primitive
{
    vec2 x0;          // transform (local units -> canvas pixels): pixel = x0 * lx + x1 * ly + x2
    vec2 x1;
    vec2 x2;
    vec2 size;        // the box, from (0,0) in local units
    vec4 radiiX;      // corner radii: top left, top right, bottom right, bottom left
    vec4 radiiY;
    vec4 border;      // border widths: top, right, bottom, left
    vec4 color;       // premultiplied fill colour / tint
    vec4 borderColor; // premultiplied
    vec4 uv;          // texture corners u0 v0 u1 v1
    vec4 clip;        // clip rectangle in canvas pixels: x0 y0 x1 y1
    vec4 clipRadii;   // clip corner radii in pixels: tl tr br bl
    vec4 params;      // shadow: offset.xy, spread, sigma; dashed border: dash, gap
    uint kind;
    uint textureIndex;
    uint samplerIndex;
    uint brush;       // gradient index or NO_INDEX
    uint colorMatrix; // colour matrix index or NO_INDEX
    uint flags;
    uint pad0;
    uint pad1;
};

struct Gradient
{
    uint kind;        // 0 linear, 1 radial
    uint count;
    uint repeating;
    uint pad;
    vec4 geometry;    // linear: start.xy end.xy; radial: center.xy radii.xy (local units)
    vec4 colors[16];  // premultiplied
    float offsets[16];
};

struct ColorMatrixData
{
    mat4 m;           // applied as m * color (straight colour)
    vec4 offset;
};

layout(buffer_reference, scalar) readonly buffer Primitives { Primitive items[]; };
layout(buffer_reference, scalar) readonly buffer Gradients { Gradient items[]; };
layout(buffer_reference, scalar) readonly buffer ColorMatrices { ColorMatrixData items[]; };

layout(push_constant, scalar) uniform Push
{
    Primitives primitives;
    Gradients gradients;
    ColorMatrices matrices;
    vec2 targetSize;    // pixels
    vec2 targetOrigin;  // canvas pixel at the target's top left (layers)
} pc;

// A box shadow's blur (params.w, its sigma in the box's units) in the box's units: the sigma along x,
// along y, and their correlation. Blink draws a shadow with a draw looper whose blur respects the
// transform (kShadowRespectsTransforms), and Skia's blur mask filter takes its sigma to the device
// with one factor for the whole matrix (SkBlurMaskFilterImpl::computeXformedSigma: SkMatrix::mapRadius,
// the geometric mean of the lengths the matrix gives a step along x and along y, at most 128) and blurs
// the shape as the transform places it on the device, the same in every direction. Here the shape
// stays in the box's units, where that blur is a Gaussian of covariance device² (M⁻¹)(M⁻¹)ᵀ, M the
// matrix's columns x0 and x1: local x changes |x1| / |det| a pixel (the gradient of x is the inverse's
// first row), local y |x0| / |det|, and the two are correlated by -(x0 · x1) / (|x0| |x1|), which is
// zero unless the transform shears the box's axes (a scale after a rotation, a skew).
vec3 shadowBlur(Primitive p)
{
    float sigma = p.params.w;
    float alongX = length(p.x0);
    float alongY = length(p.x1);
    float device = min(sigma * sqrt(alongX * alongY), 128.0);
    float det = abs(p.x0.x * p.x1.y - p.x0.y * p.x1.x);
    if (det <= 0.0)
        return vec3(0.0);
    return vec3(device * vec2(alongY, alongX) / det, -dot(p.x0, p.x1) / (alongX * alongY));
}
