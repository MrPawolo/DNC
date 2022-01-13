using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ML.DNCHandler.Data
{
    public enum TextureType
    {
        Lightmap,
        Shadowmap
    }
    //[CreateAssetMenu(fileName = "LightData", menuName = "ML/DNC/LightData")]
    public class LightData : ScriptableObject
    {
        public LightDataStorage lightDataStorage;

        /*
        public Texture2D[] lightmaps;
        public Texture2D[] shadowMaps;
        public LightProbes lightProbes;
        public Light sun;
        public Light[] additionalLights;
        public Quaternion sunRotation;
        public Color ambientColor;
        public float hour;
        public string holderPath;
        public string sceneName;*/


#if UNITY_EDITOR
        #region SetUpData
        public bool Init(LightDataProperties _lightDataProperties)
        {
            AssetDatabase.Refresh();
            //----miscellaneous------
            lightDataStorage.hour = _lightDataProperties.hour;
            lightDataStorage.dataFolderPath = _lightDataProperties.dataFolderPath;
            lightDataStorage.sceneName = _lightDataProperties.sceneName;
            string newHour = _lightDataProperties.hour.ToString().Replace(".", ","); //Changed because loading data from disk is "." sensitive
            lightDataStorage.assetFolderPath = _lightDataProperties.dataFolderPath + "/LightData_" + newHour;

            //-------lighting--------
            lightDataStorage.ambientColor = _lightDataProperties.ambientColor;
            //sun
            Light sun = _lightDataProperties.sun;
            lightDataStorage.sunData.rotation = sun.transform.rotation;
            lightDataStorage.sunData.color = sun.color;
            lightDataStorage.sunData.intensity = sun.intensity;
            lightDataStorage.sunData.isMoon = _lightDataProperties.isMoon;
            //additionalLights
            CreateAdditionalLightsData(_lightDataProperties);


            //Prepare Data patah
            string unityProjectPathBase = Application.dataPath.Replace("Assets", "");
            string assetFullDirectory = unityProjectPathBase + lightDataStorage.assetFolderPath;
            CheckIfDirectoryExistAndCreate(assetFullDirectory);

            //Creating Assets
            lightDataStorage.lightProbesHolderPath = CreateLightProbeHolder(_lightDataProperties);
            SaveTextures(_lightDataProperties.lightmaps, ref lightDataStorage.lightmapsPath, TextureType.Lightmap);
            SaveTextures(_lightDataProperties.shadowMaps, ref lightDataStorage.shadowMapsPath, TextureType.Shadowmap);
            SaveCubemaps(_lightDataProperties.ReflectionProbes, ref lightDataStorage.reflectionProbesPath);


            EditorUtility.SetDirty(this);
            return true;
        }

        private void CreateAdditionalLightsData(LightDataProperties _lightDataProperties)
        {
            Light[] additionalLights = _lightDataProperties.additionalLights;
            if(additionalLights == null) { Debug.Log("No Additional Lights"); return; }
            
            lightDataStorage.additionalLightsDatas = new AdditionalLightsData[additionalLights.Length];
            for (int i = 0; i < additionalLights.Length; i++)
            {
                lightDataStorage.additionalLightsDatas[i].color = additionalLights[i].color;
                lightDataStorage.additionalLightsDatas[i].intensity = additionalLights[i].intensity;
            }
        }

        public bool ChangeProperties(LightDataProperties lightDataProperties)
        {
            //Destroy old assets
            //Create new assets


            Debug.LogError("Function Wasnt Implemented");
            return true;
        }





        #endregion





        private void OnDestroy()
        {
            //TODO: There is the place where every created Asset need to be destroyed
        }

        void DestroyCreatedFiles()
        {

        }



        #region AssetCreation
        private string CreateLightProbeHolder(LightDataProperties _lightDataProperties)
        {
            UnityEngine.Rendering.SphericalHarmonicsL2[] lightProbes = _lightDataProperties.lightProbesValues;
            Vector3[] lightProbesPositions = _lightDataProperties.lightProbesPositions;
            
            if(lightProbes == null || lightProbes.Length == 0) { Debug.Log("No LightProbesValues was send"); return ""; }

            LightProbesHolder lightProbesHolder = ScriptableObject.CreateInstance<LightProbesHolder>();
            lightProbesHolder.lightProbesValues = lightProbes;
            lightProbesHolder.lightProbesPositions = lightProbesPositions;
            string lighyProbesHolderPath = lightDataStorage.assetFolderPath + "/LightProbeHolder.asset";
            AssetDatabase.CreateAsset(lightProbesHolder, lighyProbesHolderPath);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(lightProbesHolder);
            return lighyProbesHolderPath;
        }

        void SaveTextures(Texture2D[] _textures, ref string[] texturePaths, TextureType _textureType)
        {
            if (_textures == null) { Debug.Log("No textures was send " + _textureType.ToString()); return; }
            texturePaths = new string[_textures.Length];
            for(int i = 0; i < _textures.Length; i++)
            {
                string fileName;
                if (_textureType == TextureType.Lightmap)
                {
                    fileName = "/Lightmap-" + i.ToString() + "_comp_light" + ".exr";
                }
                else
                {
                    fileName = "/Lightmap-" + i.ToString() + "_comp_shadowmask" + ".png";
                }

                string orgTexPath = AssetDatabase.GetAssetOrScenePath(_textures[i]);

                string inUnityPath = lightDataStorage.assetFolderPath + fileName;
                texturePaths[i] = inUnityPath;

                
                CreateAssetCopy(orgTexPath, inUnityPath);
            }
        }

        private void CreateAssetCopy(string orgTexPath, string inUnityPath)
        {
            
            AssetDatabase.CopyAsset(orgTexPath, inUnityPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void SaveCubemaps(Cubemap[] _textures, ref string[] texturePaths)
        {
            if(_textures == null) { Debug.Log("No Reflection Probes was send"); return; }
            texturePaths = new string[_textures.Length];
            for (int i = 0; i < _textures.Length; i++)
            {
                string fileName = "/ReflectionProbe_" + i.ToString() + ".exr";

                string orgTexPath = AssetDatabase.GetAssetOrScenePath(_textures[i]);

                string inUnityPath = lightDataStorage.assetFolderPath + fileName;

                texturePaths[i] = inUnityPath;

                CreateAssetCopy(orgTexPath, inUnityPath);
            }
        }

        void CheckIfDirectoryExistAndCreate(string dir)
        {
            if (Directory.Exists(dir))
            {
                return;
            }
            else
            {
                Directory.CreateDirectory(dir);
            }
        }
        #endregion
#endif

    }
}
