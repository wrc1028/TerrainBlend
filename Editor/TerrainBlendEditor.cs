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
                    RenderTexture rawIDResult = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rawBlendResult = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    source.Update2LayersBlend(true, ref rawIDResult, ref rawBlendResult);
                    rawIDResult.Release();
                    rawBlendResult.Release();
                }

                if (GUILayout.Button("Update 3 Layers(Combine)"))
                {
                    RenderTexture rawIDResult = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    RenderTexture rawBlendResult = Utils.CreateRenderTexture(source.m_TerrainAsset.m_AlphamapResolution, RenderTextureFormat.ARGB32);
                    source.Update2LayersBlend(false, ref rawIDResult, ref rawBlendResult);
                    source.Update3LayersBlend(rawIDResult, rawBlendResult);
                    rawIDResult.Release();
                    rawBlendResult.Release();
                }
            }
        }
    }
}