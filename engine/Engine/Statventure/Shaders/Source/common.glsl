// Shared by every Statventure shader: the bindless texture table (set 0) and buffer references
// (vertex data is pulled from buffers by their device address, passed in push constants).
#extension GL_EXT_nonuniform_qualifier : require
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_scalar_block_layout : require
#extension GL_EXT_shader_explicit_arithmetic_types_int64 : require

layout(set = 0, binding = 0) uniform texture2D Textures[];
layout(set = 0, binding = 1) uniform sampler Samplers[];

const uint SAMPLER_LINEAR_CLAMP = 0;
const uint SAMPLER_NEAREST_CLAMP = 1;
const uint SAMPLER_LINEAR_REPEAT = 2;
const uint SAMPLER_NEAREST_REPEAT = 3;
const uint SAMPLER_ANISOTROPIC_REPEAT = 4;
const uint SAMPLER_LINEAR_MIRROR = 5;

vec4 sampleTexture(uint textureIndex, uint samplerIndex, vec2 uv)
{
    return texture(sampler2D(Textures[nonuniformEXT(textureIndex)], Samplers[nonuniformEXT(samplerIndex)]), uv);
}

vec4 sampleTextureLod(uint textureIndex, uint samplerIndex, vec2 uv, float lod)
{
    return textureLod(sampler2D(Textures[nonuniformEXT(textureIndex)], Samplers[nonuniformEXT(samplerIndex)]), uv, lod);
}
