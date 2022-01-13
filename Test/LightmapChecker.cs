using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Threading;
using System.IO;
using ML.DNCHandler.Data;

namespace ML.DNCHandler
{
    //[System.Serializable]
    //public struct LightData
    //{
    //    public Texture2D[] lightmaps;
    //    public Texture2D[] shadowMaps;
    //    public LightProbes lightProbes;
    //    public Light sun;
    //    public Light[] additionalLights;
    //    public Quaternion sunRotation;
    //    public Color ambientColor;
    //    public float hour;
    //}
    public class LightmapChecker : MonoBehaviour
    {
        public LightData _lightData; // Test solution

        //public LightData[] lightData;
        public Texture2D[] lightmaps;
        public List<Texture2D> lightmaps_Shadow = new List<Texture2D>();
        public LightProbes lightProbes;
        public SphericalHarmonicsL2[] sphericalsHarmonics;

        [Header("---Test---")]

        [Range(0, 1)]public  float val = 0;
        public SphericalHarmonicsL2 sphericalHarmonicsL2;

        public Transform firstTrans;
        public Transform secondTrans;
        public Transform SunTrans;

        public Texture2D first;
        public Texture2D second;
        public RenderTexture renderTex;
        public Material lerpMat;
        Texture2D tex;

        float progress;

        [ContextMenu("GetLightmaps")]
        public void GetLightMaps()
        {
            LightmapData[] lightmapsData = LightmapSettings.lightmaps;

            lightmaps = new Texture2D[lightmapsData.Length];

            for(int i = 0; i < lightmapsData.Length; i++)
            {
                lightmaps[i] = lightmapsData[i].lightmapColor;
            }
            //_lightData.SetLightMaps(LightmapSettings.lightmaps);
            //SetLightMaps(lightmaps);
            //foreach(LightmapData lightmap in lightmapsData)
            //{
            //    lightmaps.Add(lightmap.lightmapColor);
            //}
            lightmaps_Shadow.Clear();
            foreach (LightmapData lightmap in lightmapsData)
            {
                lightmaps_Shadow.Add(lightmap.shadowMask);
            }

        }
        [ContextMenu("UnpickLightmap")]
        public void UnpickLightmap()
        {
            LightmapData[] lightmapsData = LightmapSettings.lightmaps;

            for (int i = 0; i < lightmapsData.Length; i++)
            {
                LightmapSettings.lightmaps[i].lightmapColor.name = "LightMap_" + i;
            }

            LightmapSettings.lightmaps = null;
        }

        [ContextMenu("SwitchLightMaps")]
        public void SwitchToBaked()
        {
            LightmapData[] lightmapsData = LightmapSettings.lightmaps;

            for(int i = 0; i < lightmapsData.Length; i++)
            {

                //var old_rt = RenderTexture.active;
                //RenderTexture.active = renderTex;
                //tex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                //tex.Apply();
                //RenderTexture.active = old_rt;

                Graphics.CopyTexture(renderTex, tex);

                progress += Time.deltaTime * val;
                if(progress > 1 ) { progress = 0; }

                
                lightmapsData[i].lightmapColor = tex;
            }
            LightmapSettings.lightmaps = lightmapsData;
        }

        [ContextMenu("GetLightProbes")]
        public void GetLightProbes()
        {
            lightProbes = LightmapSettings.lightProbes;
            sphericalsHarmonics = lightProbes.bakedProbes;
        }
        /// <summary>
        /// This function lerp the SphericalHarmonicsL2, works also on thread
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public SphericalHarmonicsL2 LerpSphericalHarmonics(SphericalHarmonicsL2 a, SphericalHarmonicsL2 b, float t)
        {
            return (1 - t) * a + b * t;
        }
        private void Update()
        {

            lerpMat.SetFloat("_Val", val);

            
            lerpMat.SetTexture("_First", first);
            lerpMat.SetTexture("_Second", second);
            RenderTexture temp = RenderTexture.GetTemporary(first.width, second.height, 0, RenderTextureFormat.DefaultHDR);
            
            Graphics.Blit(renderTex, temp);
            Graphics.Blit(temp, renderTex, lerpMat);
            RenderTexture.ReleaseTemporary(temp);
            SwitchToBaked();

            SunTrans.rotation = Quaternion.Lerp(firstTrans.rotation, secondTrans.rotation, val);
        }
        Thread myThread;
        private void Start()
        {
            //myThread = new Thread(TextureTest);
            //myThread.Start();
            renderTex = new RenderTexture(first.width, second.height, 0, RenderTextureFormat.ARGBFloat);
            tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGBAFloat, false);
        }
        void TextureTest()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            while (true)
            {
                //var old_rt = RenderTexture.active;
                //RenderTexture.active = renderTex;

                tex.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                tex.Apply();
                //RenderTexture.active = old_rt;
            }
            sw.Stop();
        }
        void Job()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            sphericalHarmonicsL2 = LerpSphericalHarmonics(sphericalsHarmonics[9], sphericalsHarmonics[10], val);

            sw.Stop();
        }
    }
}
