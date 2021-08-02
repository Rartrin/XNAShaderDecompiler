﻿namespace XNAShaderDecompiler
{
    public enum SymbolType:uint
    {
        Void = 0,
        Bool,
        Int,
        Float,
        String,
        Texture,
        Texture1D,
        Texture2D,
        Texture3D,
        TextureCube,
        Sampler,
        Sampler1D,
        Sampler2D,
        Sampler3D,
        SamplerCube,
        PixelShader,
        VertexShader,
        PixelFragment,
        VertexFragment,
        Unsupported,
        Total /* housekeeping value; never returned. */
    }
}