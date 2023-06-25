using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace TerrainBlend16
{
    [ExecuteInEditMode]
    public class TerrainBlend : MonoBehaviour
    {
        public TerrainBlendAsset m_TerrainAsset;
        
        public void Update2LayersBlend(bool outputDebugTexture, ref RenderTexture rawIDResult, ref RenderTexture rawBlendResult)
        {
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            // R: 地形混合层数; G: 处理贴图分辨率; B: 混合模式; A: 地形索引
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 0, 0);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));

            // 输出原始的IDTexture贴图
            RenderTexture rawIDTexture = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchRawIDTexture(m_TerrainAsset, indexRank, ref rawIDTexture);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDTexture, TextureFormat.RGBA32, GetTextureSavePath("2Layers/1RawIDTexture.tga", m_TerrainAsset.m_TerrainName));

            // 擦除双层结构层次以外的RawIDMaskArray
            RenderTexture rawIDMaskArrayErase = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskErase(m_TerrainAsset, new Vector4(0, 1, 0, 2), indexRank, rawIDTexture, ref rawIDMaskArrayErase);
            // 扩展擦除后的RawIDMask
            RenderTexture rawIDMaskArrayExtend = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskExtend(m_TerrainAsset, rawIDMaskArrayErase, ref rawIDMaskArrayExtend);
            // 在有重叠的位置进行判断
            RenderTexture rawIDLayerEdge = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckIDLayerEdge(m_TerrainAsset, rawIDMaskArrayExtend, ref rawIDLayerEdge);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDLayerEdge, TextureFormat.RGBA32, GetTextureSavePath("2Layers/2RawIDLayerEdge.tga", m_TerrainAsset.m_TerrainName));
            // 输出 RawIDResult
            // RenderTexture rawIDResult = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchTransformG2B(m_TerrainAsset, rawIDTexture, rawIDLayerEdge, ref rawIDResult);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawIDResult, TextureFormat.RGBA32, GetTextureSavePath("2Layers/3RawIDResult.tga", m_TerrainAsset.m_TerrainName));
            // 输出双层混合结构的 RawBlendResult
            // RenderTexture rawBlendResult = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchDoubleLayersBlend(m_TerrainAsset, rawIDResult, rawIDLayerEdge, ref rawBlendResult);
            if (outputDebugTexture)
                Utils.SaveRT2Texture(rawBlendResult, TextureFormat.RGBA32, GetTextureSavePath("2Layers/4RawBlendResult.tga", m_TerrainAsset.m_TerrainName));
            
            // Debug RenderTextureArray
            // for (int i = 0; i < m_TerrainAsset.m_LayersCount; i++)
            // {
            //     RenderTexture rawIDMaskErase = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            //     Graphics.CopyTexture(rawIDMaskArrayExtend, i, 0, rawIDMaskErase, 0, 0);
            //     Utils.SaveRT2Texture(rawIDMaskErase, TextureFormat.ARGB32, GetTextureSavePath($"2Layers/Debug/rawIDMaskErase{i}.tga", m_TerrainAsset.m_TerrainName));
            //     rawIDMaskErase.Release();
            // }
            indexRank.Release();
            rawIDTexture.Release();
            rawIDMaskArrayErase.Release();
            rawIDMaskArrayExtend.Release();
            rawIDLayerEdge.Release();
            // rawIDResult.Release();
            // rawBlendResult.Release();
            AssetDatabase.Refresh();
        }
        public void Update3LayersBlend(RenderTexture rawIDTexture, RenderTexture rawBlendTexture)
        {
            if (m_TerrainAsset == null || BlendCS2LUtils.Compute == null) return;
            m_TerrainAsset.m_ThreadGroups = Mathf.CeilToInt(m_TerrainAsset.m_AlphamapResolution / 8);
            BlendCS2LUtils.Compute.SetInts(ShaderParams.s_TerrainParamsID, m_TerrainAsset.m_LayersCount, m_TerrainAsset.m_AlphamapResolution, 1, 0);
            ComputeBuffer indexRank = CreateAndGetBuffer(m_TerrainAsset.m_CoverageIndexRank, sizeof(uint));
            // 擦除三层结构层次以外的RawIDMaskArray, 注意区分G2B的区域
            RenderTexture rawIDMaskArrayEraseG = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskErase(m_TerrainAsset, new Vector4(0, 1, 0, 3), indexRank, rawIDTexture, ref rawIDMaskArrayEraseG);
            RenderTexture rawIDMaskArrayEraseB = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskErase(m_TerrainAsset, new Vector4(0, 0, 1, 3), indexRank, rawIDTexture, ref rawIDMaskArrayEraseB);
            // 扩展擦除后的RawIDMask
            RenderTexture rawIDMaskArrayExtendG = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskExtend(m_TerrainAsset, rawIDMaskArrayEraseG, ref rawIDMaskArrayExtendG);
            RenderTexture rawIDMaskArrayExtendB = Utils.CreateRenderTexture3D(m_TerrainAsset.m_AlphamapResolution, m_TerrainAsset.m_LayersCount, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchIDMaskExtend(m_TerrainAsset, rawIDMaskArrayEraseB, ref rawIDMaskArrayExtendB);
            // 在有重叠的位置进行判断
            RenderTexture rawIDLayerEdgeG = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckIDLayerEdge(m_TerrainAsset, rawIDMaskArrayExtendG, ref rawIDLayerEdgeG);
            RenderTexture rawIDLayerEdgeB = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS2LUtils.DispatchCheckIDLayerEdge(m_TerrainAsset, rawIDMaskArrayExtendB, ref rawIDLayerEdgeB);
            Utils.SaveRT2Texture(rawIDLayerEdgeG, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1_1RawIDLayerEdgeG.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(rawIDLayerEdgeB, TextureFormat.RGBA32, GetTextureSavePath("3Layers/1_2RawIDLayerEdgeB.tga", m_TerrainAsset.m_TerrainName));
            // 查找可以转移到A通道的ID, 比如: 
            // 1、在相同区域内GB都存在的ID;
            // 2、混合值相似的情况
            RenderTexture sameLayerIDG = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCheckSameLayerID(m_TerrainAsset, rawIDLayerEdgeG, rawIDLayerEdgeB, ref sameLayerIDG);
            RenderTexture sameLayerIDB = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCheckSameLayerID(m_TerrainAsset, rawIDLayerEdgeB, rawIDLayerEdgeG, ref sameLayerIDB);
            Utils.SaveRT2Texture(sameLayerIDG, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2_1SameLayerIDG.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(sameLayerIDB, TextureFormat.RGBA32, GetTextureSavePath("3Layers/2_2SameLayerIDB.tga", m_TerrainAsset.m_TerrainName));
            
            CheckSameLayerID(ref sameLayerIDG);
            CheckSameLayerID(ref sameLayerIDB);
            Utils.SaveRT2Texture(sameLayerIDG, TextureFormat.RGBA32, GetTextureSavePath("3Layers/3_1SameLayerIDG.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(sameLayerIDB, TextureFormat.RGBA32, GetTextureSavePath("3Layers/3_2SameLayerIDB.tga", m_TerrainAsset.m_TerrainName));
            // 处理余下的LayerID
            RenderTexture sameILayerIDG = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCheckIsolateLayerID(m_TerrainAsset, sameLayerIDG, ref sameILayerIDG);
            RenderTexture sameILayerIDB = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCheckIsolateLayerID(m_TerrainAsset, sameLayerIDB, ref sameILayerIDB);
            Utils.SaveRT2Texture(sameILayerIDG, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4_1SameLayerIDG.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(sameILayerIDB, TextureFormat.RGBA32, GetTextureSavePath("3Layers/4_2SameLayerIDB.tga", m_TerrainAsset.m_TerrainName));
            // 将两个sameILayerID合并
            RenderTexture sameLayerID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture idLayerEdgeG = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture idLayerEdgeB = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthCombineLayerID(m_TerrainAsset, sameILayerIDG, sameILayerIDB, rawIDLayerEdgeG, rawIDLayerEdgeB, ref sameLayerID, ref idLayerEdgeG, ref idLayerEdgeB);
            Utils.SaveRT2Texture(sameLayerID, TextureFormat.RGBA32, GetTextureSavePath("3Layers/5SameLayerID.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(idLayerEdgeG, TextureFormat.RGBA32, GetTextureSavePath("3Layers/5_1IDLayerEdgeG.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(idLayerEdgeB, TextureFormat.RGBA32, GetTextureSavePath("3Layers/5_2IDLayerEdgeB.tga", m_TerrainAsset.m_TerrainName));
            // 转移像素
            RenderTexture rawIDResult = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            RenderTexture alphaID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthTransformGB2A(m_TerrainAsset, rawIDTexture, sameLayerID, ref rawIDResult, ref alphaID);
            Utils.SaveRT2Texture(rawIDResult, TextureFormat.RGBA32, GetTextureSavePath("3Layers/6RawIDTexture.tga", m_TerrainAsset.m_TerrainName));
            Utils.SaveRT2Texture(alphaID, TextureFormat.RGBA32, GetTextureSavePath("3Layers/6AlphaID.tga", m_TerrainAsset.m_TerrainName));
            // 输出最终Blend贴图
            RenderTexture rawBlendResult = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
            BlendCS3LUtils.DispacthThreeLayersBlend(m_TerrainAsset, rawBlendTexture, idLayerEdgeG, idLayerEdgeB, alphaID, ref rawBlendResult);
            Utils.SaveRT2Texture(rawBlendResult, TextureFormat.RGBA32, GetTextureSavePath("3Layers/7RawBlendResult.tga", m_TerrainAsset.m_TerrainName));

            rawIDMaskArrayEraseG.Release();
            rawIDMaskArrayEraseB.Release();
            rawIDMaskArrayExtendG.Release();
            rawIDMaskArrayExtendB.Release();
            rawIDLayerEdgeG.Release();
            rawIDLayerEdgeB.Release();
            sameLayerIDG.Release();
            sameLayerIDB.Release();
            sameILayerIDG.Release();
            sameILayerIDB.Release();
            sameLayerID.Release();
            idLayerEdgeG.Release();
            idLayerEdgeB.Release();
            rawIDResult.Release();
            alphaID.Release();
            rawBlendResult.Release();
            indexRank.Release();
            AssetDatabase.Refresh();
        }
        private void CheckSameLayerID(ref RenderTexture sameLayerID)
        {
            uint[] undisposed = new uint[1] { 0 };
            uint prevUndisposed = 0;
            // 循环扩散查找
            for (int i = 0; i < 128; i++)
            {
                undisposed[0] = 0;
                ComputeBuffer undisposedCount = CreateAndGetBuffer(undisposed, sizeof(uint));
                RenderTexture sameNearLayerID = Utils.CreateRenderTexture(m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                BlendCS3LUtils.DispacthCheckNearSameLayerID(m_TerrainAsset, undisposedCount, sameLayerID, ref sameNearLayerID);
                sameLayerID.Release();
                sameLayerID = sameNearLayerID;
                undisposedCount.GetData(undisposed);
                undisposedCount.Dispose();
                undisposedCount.Release();
                
                Debug.Log(undisposed[0]);
                if (prevUndisposed == undisposed[0]) break;
                prevUndisposed = undisposed[0];
            }
        }
        public string GetTextureSavePath(string texName, string terrainName)
        {
            return $"{Utils.s_RootPath}/TerrainAsset/{terrainName}/Debug/{texName}";
        }
        private ComputeBuffer CreateAndGetBuffer(System.Array data, int stride)
        {
            ComputeBuffer tempBuffer = new ComputeBuffer(data.Length, stride);
            tempBuffer.SetData(data);
            return tempBuffer;
        }
    }
}
