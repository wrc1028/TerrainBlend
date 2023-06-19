using UnityEngine;
using UnityEditor;

namespace TerrainBlend16
{
    [CustomEditor(typeof(TerrainBlend)), CanEditMultipleObjects]
    public class TerrainBlendEditor : Editor 
    {
        public override void OnInspectorGUI() 
        {
            TerrainBlend source = target as TerrainBlend;
            base.OnInspectorGUI();
            if (source.m_TerrainAsset != null)
            {
                if (GUILayout.Button("Update 2 Layers"))
                {
                    RenderTexture rawIDTexture = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rawIDResult = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rChannelMask = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    source.Update2LayersBlend(true, ref rawIDTexture, ref rawIDResult, ref rChannelMask);
                    rawIDTexture.Release();
                    rawIDResult.Release();
                    rChannelMask.Release();
                }
                
                if (GUILayout.Button("Update 3 Layers(Split)"))
                {
                    RenderTexture rawIDTexture = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rawIDResult = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rChannelMask = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    source.Update2LayersBlend(false, ref rawIDTexture, ref rawIDResult, ref rChannelMask);
                    source.Update3LayersBlend(rawIDTexture);
                    rawIDTexture.Release();
                    rawIDResult.Release();
                    rChannelMask.Release();
                }

                if (GUILayout.Button("Update 3 Layers(Combine)"))
                {
                    // 原始rawID和双层混合处理之后的rawID在:
                    // 1、R通道, 标记了转移的像素(Mask ==> 0)
                    // 2、B通道, 继承了从G通道转移而来的ID
                    // 3、A通道, 转移像素的混合结构从双层混合变为三层混合
                    RenderTexture rawIDTexture = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rawIDResult = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rChannelMask = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    source.Update2LayersBlend(false, ref rawIDTexture, ref rawIDResult, ref rChannelMask);
                    source.Update3LayersBlend(rawIDResult);
                    rawIDTexture.Release();
                    rawIDResult.Release();
                    rChannelMask.Release();
                }
            }
        }
    }
}