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
