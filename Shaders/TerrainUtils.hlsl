#ifndef TERRAIN_BLEND_UTILS_INCLUDE
#define TERRAIN_BLEND_UTILS_INCLUDE

uint4 _TerrainParams;
#define _LayerCount _TerrainParams.x // 地形层数
#define _Resolution _TerrainParams.y // 地形Blend贴图的分辨率
#define _LayerIndex _TerrainParams.z // 当前地形索引
#define _BlendMode  _TerrainParams.w // 区分二、三层混合

float4 _ExtendParams;
StructuredBuffer<uint> _IndexRank;
Texture2DArray<float> _AlphaTextureArray;

Texture2D<float4> _TextureInput01;
Texture2D<float4> _TextureInput02;
Texture2D<float4> _TextureInput03;
Texture2D<float4> _TextureInput04;

RWTexture2D<float4> Result01;
RWTexture2D<float4> Result02;
RWTexture2D<float4> Result03;
RWTexture2D<float4> Result04;

static const int2 _Offset[8] = 
{
    int2(-1, 1),  int2(0, 1),  int2(1, 1),
    int2(-1, 0),               int2(1, 0),
    int2(-1, -1), int2(0, -1), int2(1, -1), 
};
static const int2 _CrossOffset[4] = 
{
    int2(0, 1), int2(-1, 0), int2(1, 0), int2(0, -1),
};
float Encode(uint layerIndex, int addTag)
{
    return ((layerIndex << 4) + addTag) / 255.0;
}
int Decode(float idValue)
{
    int tempID = floor(idValue * 255.0);
    return tempID >> 4;
}
int2 DecodeMask(float idValue)
{
    int tempID = floor(idValue * 255.0);
    int layerIndex = tempID >> 4;
    return int2(layerIndex, tempID - layerIndex * 16);
}
uint2 GetNearID(uint2 id, uint index)
{
    return clamp(id + _Offset[index], 0, _Resolution - 1);
}
uint2 GetNearID(uint2 id, int2 offset)
{
    return clamp(id + offset, 0, _Resolution - 1);
}
uint2 GetCrossID(uint2 id, uint index)
{
    return clamp(id + _CrossOffset[index], 0, _Resolution - 1);
}

#endif