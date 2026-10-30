#version 460
#include "canvas_fragment.glsl"

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
        float shadow = blurredBox(lower, upper, local, p.params.w, corner);
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
        float hole = (upper.x > lower.x && upper.y > lower.y) ? blurredBox(lower, upper, local, p.params.w, corner) : 0.0;
        color = p.color * box * (1.0 - hole);
    }
    else
    {
        float shape = boxCoverage(local, p.size, p.radiiX, p.radiiY);
        vec2 t = clamp(local / max(p.size, vec2(1e-4)), 0.0, 1.0);
        vec2 uv = mix(p.uv.xy, p.uv.zw, t);
        vec4 texel = sampleTexture(p.textureIndex, p.samplerIndex, uv);
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
