using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ML.DNCHandler.Data
{
    public struct LightDataProperties
    {
        public Texture2D[] lightmaps;
        public Texture2D[] shadowMaps;
        public SphericalHarmonicsL2[] lightProbesValues;
        public Vector3[] lightProbesPositions;
        public Cubemap[] ReflectionProbes; //TODO: needChange To custom Struct
        public Light sun; //TODO: needChange To custom Struct
        public bool isMoon;
        public Light[] additionalLights; //TODO:needChange To custom Struct
        public Color ambientColor;
        public float hour;
        public string dataFolderPath;
        public string sceneName;
    }
    [System.Serializable]
    public class LightDataHandle
    {
        public SunData sunData;
        public AdditionalLightsData[] additionalLightsDatas;
        public Color ambientColor;
    }


    [System.Serializable]
    public struct LightDataStorage
    {
        public string[] lightmapsPath;
        public string[] shadowMapsPath;
        public string[] reflectionProbesPath;
        public string lightProbesHolderPath;
        public SunData sunData;
        public AdditionalLightsData[] additionalLightsDatas;

        [ColorUsage(false, true)]
        public Color ambientColor;
        public float hour;
        public string dataFolderPath;
        public string assetFolderPath;
        public string sceneName;
    }
    [System.Serializable]
    public struct SunData
    {
        public Quaternion rotation;
        public float intensity;
        public Color color;
        public bool isMoon;
    }
    [System.Serializable]
    public struct AdditionalLightsData
    {
        public float intensity;
        public Color color;
    }

    [System.Serializable]
    public struct PerHourSettings
    {
        public string name;
        public SunData sunData;
        public List<AdditionalLightsData> additionalLightsDatas;
        [ColorUsage(false, true)]
        public Color ambienColor;
        [Range(0, 24)] public float hour;
    }

    [System.Serializable]
    public class DNCTexture2DArray
    {
        public Texture2D[] textures;
    }
    [System.Serializable]
    public class DNCCubemapArray
    {
        public Cubemap[] cubemaps;
    }

    [System.Serializable]
    public class DNCTexture2DArrayNew
    {
        public Dictionary<string,Texture2D> texturesLookup = new Dictionary<string, Texture2D>(); //not ordered way 
    }
    [System.Serializable]
    public class DNCCubemapArrayNew
    {
        public List<Cubemap> cubemaps = new List<Cubemap>();
    }

}
