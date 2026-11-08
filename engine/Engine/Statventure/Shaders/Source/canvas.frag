#version 460
#include "canvas_fragment.glsl"
#extension GL_EXT_samplerless_texture_functions : require

// Skia's raster sampling of an image with linear filtering through its legacy shader context (Chromium builds it:
// SK_ENABLE_LEGACY_SHADERCONTEXT): the pixel's centre mapped to the texture (SkBitmapProcStateAutoMapper), less half a
// texel (the filter's bias), its integer part the first texel and the top four bits of its fraction the weight of
// the second (SkBitmapProcState_matrixProcs.cpp, pack: clamped to the texture, or wrapped for a pattern); the four
// texels weighed in sixteenths along x, then along y, summed in integers and divided by 256, truncated
// (S32_alpha_D32_filter_DX, SkBitmapProcState_opts.h).
// (Samplerless: a texel fetch through a sampler2D built from the bindless arrays lost its non-uniform index on RADV, the
// lanes of a wave drawing different images reading one image's texels.)
ivec4 texel8(uint textureIndex, ivec2 at)
{
    return ivec4(round(texelFetch(Textures[nonuniformEXT(textureIndex)], at, 0) * 255.0));
}

vec4 skiaBilerp(Primitive p, vec2 pixel)
{
    vec2 texel = vec2(dot(p.params.xy, pixel) + p.border.x, dot(p.params.zw, pixel) + p.border.y);
    ivec2 size = textureSize(Textures[nonuniformEXT(p.textureIndex)], 0);
    vec2 v = texel - 0.5;
    vec2 low = floor(v);
    ivec2 w = ivec2(floor((v - low) * 16.0));
    ivec2 i0 = ivec2(low), i1 = i0 + 1;
    if (p.samplerIndex == SAMPLER_LINEAR_REPEAT)
    {
        i0 = ((i0 % size) + size) % size;
        i1 = ((i1 % size) + size) % size;
    }
    else
    {
        i0 = clamp(i0, ivec2(0), size - 1);
        i1 = clamp(i1, ivec2(0), size - 1);
    }
    ivec4 a0 = texel8(p.textureIndex, ivec2(i0.x, i0.y));
    ivec4 a1 = texel8(p.textureIndex, ivec2(i1.x, i0.y));
    ivec4 b0 = texel8(p.textureIndex, ivec2(i0.x, i1.y));
    ivec4 b1 = texel8(p.textureIndex, ivec2(i1.x, i1.y));
    ivec4 top = a0 * (16 - w.x) + a1 * w.x;
    ivec4 bottom = b0 * (16 - w.x) + b1 * w.x;
    return vec4((top * 16 + (bottom - top) * w.y) >> 8) / 255.0;
}

layout(location = 0) in vec2 vLocal;
layout(location = 1) flat in uint vIndex;
layout(location = 0) out vec4 outColor;

void main()
{
    Primitive p = pc.primitives.items[vIndex];
    vec2 local = vLocal;
    vec4 color = vec4(0.0);

    if (p.kind == KIND_BOX)
    {
        float outer = boxCoverage(local, p.size, p.radiiX, p.radiiY);
        vec4 fill = p.brush == NO_INDEX ? p.color : gradientColor(p.brush, local);
        color = fill * outer;
        if ((p.borderColor.a > 0.0 || p.pad0 != NO_INDEX) && any(greaterThan(p.border, vec4(0.0))))
        {
            vec2 innerOrigin = vec2(p.border.w, p.border.x);
            vec2 innerSize = p.size - vec2(p.border.y + p.border.w, p.border.x + p.border.z);
            vec4 irx = max(p.radiiX - vec4(p.border.w, p.border.y, p.border.y, p.border.w), 0.0);
            vec4 iry = max(p.radiiY - vec4(p.border.x, p.border.x, p.border.z, p.border.z), 0.0);
            float inner = innerSize.x > 0.0 && innerSize.y > 0.0 ? boxCoverage(local - innerOrigin, innerSize, irx, iry) : 0.0;
            float band = clamp(outer - inner, 0.0, 1.0);
            vec4 borderColor = p.borderColor;
            bool dashed = (p.flags & FLAG_DASHED) != 0u;
            if (p.pad0 != NO_INDEX)
            {
                // Sides of different colours meet on the diagonals from the outer to the inner corners.
                Gradient sides = pc.gradients.items[p.pad0];
                vec4 w = p.border;
                float big = 1e9;
                float dt = w.x > 0.0 ? local.y / w.x : big;
                float dr = w.y > 0.0 ? (p.size.x - local.x) / w.y : big;
                float db = w.z > 0.0 ? (p.size.y - local.y) / w.z : big;
                float dl = w.w > 0.0 ? local.x / w.w : big;
                uint side = 0u;
                float best = dt;
                if (dr < best) { best = dr; side = 1u; }
                if (db < best) { best = db; side = 2u; }
                if (dl < best) { best = dl; side = 3u; }
                borderColor = sides.colors[side];
                dashed = ((sides.pad >> side) & 1u) != 0u;
            }
            if (dashed)
                band *= dashMask(local, p.size, p.border, p.params.x, p.params.y);
            color = borderColor * band + color * (1.0 - borderColor.a * band);
        }
    }
    else if (p.kind == KIND_SHADOW)
    {
        vec2 lower = p.params.xy - p.params.z;
        vec2 upper = p.params.xy + p.size + p.params.z;
        float corner = max(max(p.radiiX.x, p.radiiX.y), max(p.radiiX.z, p.radiiX.w));
        corner = corner > 0.0 ? max(corner + p.params.z, 0.0) : 0.0;
        float shadow = blurredBox(lower, upper, local, shadowBlur(p), corner);
        float box = boxCoverage(local, p.size, p.radiiX, p.radiiY);
        color = p.color * shadow * (1.0 - box);
    }
    else if (p.kind == KIND_INSET_SHADOW)
    {
        float box = boxCoverage(local, p.size, p.radiiX, p.radiiY);
        vec2 lower = p.params.xy + p.params.z;
        vec2 upper = p.params.xy + p.size - p.params.z;
        float corner = max(max(p.radiiX.x, p.radiiX.y), max(p.radiiX.z, p.radiiX.w));
        corner = max(corner - p.params.z, 0.0);
        if (upper.x > lower.x && upper.y > lower.y)
        {
            vec3 blur = shadowBlur(p);
            float hole = blurredBox(lower, upper, local, blur, corner);
            // Blink casts an inset shadow from a frame, not from everything outside the hole: the padding
            // box outset by the blur radius (and by the spread's size when it is negative), joined with
            // that moved back by the offset, minus the hole, all moved by the offset
            // (BoxPainterBase::PaintInsetBoxShadow, AreaCastingShadowInHole). The frame is blurred whole,
            // so the shadow is the frame's blur less the hole's. Where a transform shrinks an axis the
            // frame is thinner than the blur on the device, and the shadow along those edges lighter.
            // Without a blur the frame's outer edge is the box's own and changes nothing. The frame is
            // sampled in eight rows, not four: it spans the whole blur, and the shadow is a difference of
            // two blurs, whose errors add.
            float reach = 2.0 * p.params.w + max(-p.params.z, 0.0);
            vec2 casterLower = min(p.params.xy, 0.0) - reach;
            vec2 casterUpper = p.size + max(p.params.xy, 0.0) + reach;
            float caster = p.params.w > 0.0 ? blurredBoxRows(casterLower, casterUpper, local, blur, 0.0, 8) : 1.0;
            color = p.color * box * clamp(caster - hole, 0.0, 1.0);
        }
        else
            // No hole left: Blink fills the padding box with the shadow's colour.
            color = p.color * box;
    }
    else
    {
        float shape = boxCoverage(local, p.size, p.radiiX, p.radiiY);
        vec2 t = clamp(local / max(p.size, vec2(1e-4)), 0.0, 1.0);
        vec2 uv = mix(p.uv.xy, p.uv.zw, t);
        vec4 texel = (p.flags & FLAG_SKIA_IMAGE) != 0u
            ? skiaBilerp(p, gl_FragCoord.xy + pc.targetOrigin)
            : sampleTexture(p.textureIndex, p.samplerIndex, uv);
        if (p.kind == KIND_IMAGE)
            color = vec4(texel.rgb * texel.a, texel.a) * p.color;
        else if (p.kind == KIND_COVERAGE)
        {
            float cov = texel.r;
            color = (p.brush == NO_INDEX ? p.color : gradientColor(p.brush, local)) * cov;
        }
        else if (p.kind == KIND_COMPOSITE)
            color = texel * p.color.a;
        else
            color = p.color * texel.a;
        color *= shape;
    }

    color = applyMatrix(p.colorMatrix, color);
    color *= clipCoverage(gl_FragCoord.xy + pc.targetOrigin, p.clip, p.clipRadii);
    outColor = color;
}
