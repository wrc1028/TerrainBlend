using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace TerrainBlend16
{
    public class ShaderParams
    {
        // TODO: 设置成通用: 以后擦除、双层混合和三层混合都用这个
        public static int s_IndexRankID = Shader.PropertyToID("_IndexRank");
        public static int s_ExtendParamsID = Shader.PropertyToID("_ExtendParams");
        public static int s_TerrainParamsID = Shader.PropertyToID("_TerrainParams");
        public static int s_RawIDMaskArrayID = Shader.PropertyToID("_RawIDMaskArray");
        public static int s_AlphaTextureArrayID = Shader.PropertyToID("_AlphaTextureArray");

        public static int s_TexInput1ID = Shader.PropertyToID("_TextureInput01");
        public static int s_TexInput2ID = Shader.PropertyToID("_TextureInput02");
        public static int s_TexInput3ID = Shader.PropertyToID("_TextureInput03");
        public static int s_TexInput4ID = Shader.PropertyToID("_TextureInput04");
        
        public static int s_Result1ID = Shader.PropertyToID("Result01");
        public static int s_Result2ID = Shader.PropertyToID("Result02");
        public static int s_Result3ID = Shader.PropertyToID("Result03");
    }
    public class BlendCS2LUtils
    {
        public static ComputeShader s_Compute;
        public static ComputeShader Compute
        {
            get 
            {
                if (s_Compute == null)
                    s_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/TerrainBlend2Layers.compute");
                return s_Compute;
            }
        }
        // kernels
        public static int RawIDTexture
        {
            get { return GetKernel("RawIDTexture"); }
        }
        public static int FindIDLayerEdge
        {
            get { return GetKernel("FindIDLayerEdge"); }
        }     
        public static int CheckLayerSimilarEdge
        {
            get { return GetKernel("CheckLayerSimilarEdge"); }
        }
        public static int CheckExtendLayerEdge
        {
            get { return GetKernel("CheckExtendLayerEdge"); }
        }
        public static int TransformDimension
        {
            get { return GetKernel("TransformDimension"); }
        }
        public static int IDLayerExtend
        {
            get { return GetKernel("IDLayerExtend"); }
        }
        public static int DoubleLayersBlend
        {
            get { return GetKernel("DoubleLayersBlend"); }
        }
        private static int GetKernel(string kernelName)
        {
            if (Compute == null) return -1;
            else return Compute.FindKernel(kernelName);
        }
        // Dispatch
        public static void DispatchRawIDTexture(TerrainBlendAsset asset, ComputeBuffer indexRankBuffer, ref RenderTexture rawIDTexture)
        {
            Compute.SetBuffer (RawIDTexture, ShaderParams.s_IndexRankID, indexRankBuffer);
            Compute.SetTexture(RawIDTexture, ShaderParams.s_RawIDMaskArrayID, asset.m_RawIDMaskArray);
            Compute.SetTexture(RawIDTexture, ShaderParams.s_AlphaTextureArrayID, asset.m_AlphaTextureArray);
            Compute.SetTexture(RawIDTexture, ShaderParams.s_Result1ID, rawIDTexture);
            Compute.Dispatch(RawIDTexture, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispatchFindIDLayerEdge(TerrainBlendAsset asset, Vector4 extendParams, RenderTexture rawIDTexture, ref RenderTexture rawIDEdgeTexture)
        {
            Compute.SetVector (ShaderParams.s_ExtendParamsID, extendParams);
            Compute.SetTexture(FindIDLayerEdge, ShaderParams.s_TexInput1ID, rawIDTexture);
            Compute.SetTexture(FindIDLayerEdge, ShaderParams.s_AlphaTextureArrayID, asset.m_AlphaTextureArray);
            Compute.SetTexture(FindIDLayerEdge, ShaderParams.s_Result1ID, rawIDEdgeTexture);
            Compute.Dispatch(FindIDLayerEdge, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispatchCheckLayerSimilarEdge(TerrainBlendAsset asset, ComputeBuffer indexRankBuffer, RenderTexture rawIDEdgeTexture, ref RenderTexture rawIDEdgeResult)
        {
            Compute.SetBuffer (CheckLayerSimilarEdge, ShaderParams.s_IndexRankID, indexRankBuffer);
            Compute.SetTexture(CheckLayerSimilarEdge, ShaderParams.s_TexInput1ID, rawIDEdgeTexture);
            Compute.SetTexture(CheckLayerSimilarEdge, ShaderParams.s_AlphaTextureArrayID, asset.m_AlphaTextureArray);
            Compute.SetTexture(CheckLayerSimilarEdge, ShaderParams.s_Result1ID, rawIDEdgeResult);
            Compute.Dispatch(CheckLayerSimilarEdge, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispatchCheckExtendLayerEdge(TerrainBlendAsset asset, RenderTexture extendEdgeTexture, RenderTexture layerExtend, ref RenderTexture extendEdgeResult)
        {
            Compute.SetTexture(CheckExtendLayerEdge, ShaderParams.s_TexInput1ID, extendEdgeTexture);
            Compute.SetTexture(CheckExtendLayerEdge, ShaderParams.s_TexInput2ID, layerExtend);
            Compute.SetTexture(CheckExtendLayerEdge, ShaderParams.s_Result1ID, extendEdgeResult);
            Compute.Dispatch(CheckExtendLayerEdge, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispatchTransformDimension(TerrainBlendAsset asset, RenderTexture rawIDTexture, RenderTexture rawIDEdgeTexture, 
            ref RenderTexture rawIDResult, ref RenderTexture rawIDEdgeResult)
        {
            Compute.SetTexture(TransformDimension, ShaderParams.s_TexInput1ID, rawIDTexture);
            Compute.SetTexture(TransformDimension, ShaderParams.s_TexInput2ID, rawIDEdgeTexture);
            Compute.SetTexture(TransformDimension, ShaderParams.s_Result1ID, rawIDResult);
            Compute.SetTexture(TransformDimension, ShaderParams.s_Result2ID, rawIDEdgeResult);
            Compute.Dispatch(TransformDimension, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispatchIDLayerExtend(TerrainBlendAsset asset, Vector4 extendParams, RenderTexture rawIDTexture, ref RenderTexture layerExtend)
        {
            Compute.SetVector (ShaderParams.s_ExtendParamsID, extendParams);
            Compute.SetTexture(IDLayerExtend, ShaderParams.s_TexInput1ID, rawIDTexture);
            Compute.SetTexture(IDLayerExtend, ShaderParams.s_AlphaTextureArrayID, asset.m_AlphaTextureArray);
            Compute.SetTexture(IDLayerExtend, ShaderParams.s_Result1ID, layerExtend);
            Compute.Dispatch(IDLayerExtend, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
    }
    public class BlendCS3LUtils
    {
        private static ComputeShader s_Compute;
        public static ComputeShader Compute
        {
            get 
            {
                if (s_Compute == null)
                {
                    s_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/TerrainBlend3Layers.compute");
                    if (s_Compute == null)
                        Debug.LogError("Not found Compute Shader!");
                }
                return s_Compute;
            }
        }
        // kernels
        public static int FindTransformMask
        {
            get { return GetKernel("FindTransformMask"); }
        }
        public static int TransformDimension
        {
            get { return GetKernel("TransformDimension"); }
        }
        public static int ThreeLayersBlend
        {
            get { return GetKernel("ThreeLayersBlend"); }
        }
        private static int GetKernel(string kernelName)
        {
            if (Compute == null)
            {
                Debug.LogError("Not found Compute Shader!");
                return -1;
            }
            else return Compute.FindKernel(kernelName);
        }
        // Dispacth
        private static int ThreadGroups(TerrainBlendAsset asset)
        {
            return Mathf.CeilToInt(asset.m_AlphamapResolution / 8);
        }
        public static void DispacthFindTransformMask(TerrainBlendAsset asset, RenderTexture secondLayerEdge, 
            RenderTexture thirdLayerEdge, RenderTexture alphaMask, ref RenderTexture alphaMaskResult)
        {
            Compute.SetTexture(FindTransformMask, ShaderParams.s_TexInput1ID, secondLayerEdge);
            Compute.SetTexture(FindTransformMask, ShaderParams.s_TexInput2ID, thirdLayerEdge);
            Compute.SetTexture(FindTransformMask, ShaderParams.s_TexInput3ID, alphaMask);
            Compute.SetTexture(FindTransformMask, ShaderParams.s_Result1ID, alphaMaskResult);
            Compute.Dispatch(FindTransformMask, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispacthTransformDimension(TerrainBlendAsset asset, RenderTexture secondLayerEdge, 
            RenderTexture thirdLayerEdge, RenderTexture alphaMask, RenderTexture rawIDTexture, 
            ref RenderTexture secondLayerEdgeResult, ref RenderTexture thirdLayerEdgeResult, ref RenderTexture rawIDResult)
        {
            Compute.SetTexture(TransformDimension, ShaderParams.s_TexInput1ID, secondLayerEdge);
            Compute.SetTexture(TransformDimension, ShaderParams.s_TexInput2ID, thirdLayerEdge);
            Compute.SetTexture(TransformDimension, ShaderParams.s_TexInput3ID, alphaMask);
            Compute.SetTexture(TransformDimension, ShaderParams.s_TexInput4ID, rawIDTexture);
            Compute.SetTexture(TransformDimension, ShaderParams.s_Result1ID, secondLayerEdgeResult);
            Compute.SetTexture(TransformDimension, ShaderParams.s_Result2ID, thirdLayerEdgeResult);
            Compute.SetTexture(TransformDimension, ShaderParams.s_Result3ID, rawIDResult);
            Compute.Dispatch(TransformDimension, asset.m_ThreadGroups, asset.m_ThreadGroups, 1);
        }
        public static void DispacthThreeLayersBlend()
        {
            
        }
    }
    public class EraseShaderUtils
    {
        private static ComputeShader s_Compute;
        public static ComputeShader Compute
        {
            get 
            {
                if (s_Compute == null)
                    s_Compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        $"{Utils.s_RootPath}/Shaders/TerrainErase.compute");
                return s_Compute;
            }
        }
        // params
        public static int s_TerrainParamsID = Shader.PropertyToID("_TerrainParams");
        public static int s_EraseMaskID = Shader.PropertyToID("_EraseMask");
        public static int s_AlphaTextureID = Shader.PropertyToID("_AlphaTexture");
        public static int s_AlphaNormalizeID = Shader.PropertyToID("_AlphaNormalize");
        public static int s_AlphaTextureArrayID = Shader.PropertyToID("_AlphaTextureArray");
        public static int s_EraseMaskResultID = Shader.PropertyToID("EraseMaskResult");
        public static int s_AlphaTextureResultID = Shader.PropertyToID("AlphaTextureResult");
        public static int s_AlphaNormalizeResultID = Shader.PropertyToID("AlphaNormalizeResult");
        public static int s_AlphaTextureArrayResultID = Shader.PropertyToID("AlphaTextureArrayResult");
        // kernel
        public static int s_EraseMaskKernel
        {
            get { return GetKernel("EraseMask"); }
        }
        public static int s_EraseLayerKernel
        {
            get { return GetKernel("EraseLayer"); }
        }
        public static int s_FindIsolatedPointKernel
        {
            get { return GetKernel("FindIsolatedPoint"); }
        }
        public static int s_AppendIsolatedPointKernel
        {
            get { return GetKernel("AppendIsolatedPoint"); }
        }
        public static int s_BlurredEraseEdgeKernel
        {
            get { return GetKernel("BlurredEraseEdge"); }
        }
        public static int s_RawIDMaskKernel
        {
            get { return GetKernel("RawIDMask"); }
        }
        public static int s_AlphaNormalizeKernel
        {
            get { return GetKernel("AlphaNormalize"); }
        }
        private static int GetKernel(string kernelName)
        {
            if (Compute == null) return -1;
            else return Compute.FindKernel(kernelName);
        }
    }
    public class Utils
    {
        public static string s_RootPath = "Assets/TerrainBlend";
        public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format)
        {
            RenderTexture tempRT = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
            tempRT.filterMode = FilterMode.Point;
            tempRT.enableRandomWrite = true;
            tempRT.Create();
            return tempRT;
        }
        public static RenderTexture CreateRenderTexture3D(int size, int slicesNum, RenderTextureFormat format)
        {
            RenderTexture tempRT = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
            tempRT.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            tempRT.volumeDepth = slicesNum;
            tempRT.filterMode = FilterMode.Point;
            tempRT.enableRandomWrite = true;
            tempRT.Create();
            return tempRT;
        }
        public static void SaveRT2Texture(RenderTexture rt, TextureFormat format, string savePath, bool isHDR = false)
        {
            Texture2D tex2D = new Texture2D(rt.width, rt.height, format, false, true);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = prev;

            SaveTexture(tex2D, savePath, isHDR);
        }

        public static void SaveTexture(Texture2D tex2D, string savePath, bool isHDR = false)
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            Byte[] texBytes = isHDR ? tex2D.EncodeToEXR() : tex2D.EncodeToTGA();
            FileStream texFile = File.Open(savePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryWriter texWriter = new BinaryWriter(texFile);
            texWriter.Write(texBytes);
            texFile.Close();
            Texture2D.DestroyImmediate(tex2D);
            tex2D = null;
        }
        public static void SolveCompression(FilterMode filterMode, TextureImporterFormat format, string path, int maxTextureSize)
        {
            TextureImporter im = (TextureImporter)AssetImporter.GetAtPath(path);
            im.sRGBTexture = false;
            im.mipmapEnabled = false;
            im.filterMode = filterMode;
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.maxTextureSize = maxTextureSize;

            TextureImporterPlatformSettings androidSetting = im.GetPlatformTextureSettings("Android");
            androidSetting.overridden = true;
            androidSetting.format = format;
            im.SetPlatformTextureSettings(androidSetting);

            TextureImporterPlatformSettings iphoneSetting = im.GetPlatformTextureSettings("iPhone");
            iphoneSetting.overridden = true;
            iphoneSetting.format = format;
            im.SetPlatformTextureSettings(iphoneSetting);

            TextureImporterPlatformSettings standaloneSetting = im.GetPlatformTextureSettings("Standalone");
            standaloneSetting.overridden = true;
            standaloneSetting.format = format;
            im.SetPlatformTextureSettings(standaloneSetting);
            im.SaveAndReimport();
            AssetDatabase.Refresh();
        }
    }
}