using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using ML.DNCHandler.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ML.DNCHandler
{
    public class DNCBakeManager : MonoBehaviour
    {

        [SerializeField] LightDataHolder lightDataHolder;

        
        public List<Light> AdditionalLights { get; set; }
        public List<ReflectionProbe> ReflectionProbes { get; set; }
        public Light sun { get; set; }

        public LightDataHolder LightDataHolder { get { return lightDataHolder; } }

        public LightData GetLightData(float _hour)
        {
            if(!lightDataHolder) { Debug.Log("No light data holder"); return null;}
            if (lightDataHolder.lightDatas.ContainsKey(_hour))
            {
                return lightDataHolder.lightDatas[_hour];
            }
            Debug.Log("Key isnt valid");
            return null;
        }
        /*
        private IEnumerable<Light> AdditionalLights 
        {
            get 
            {
                foreach(Light additionalLight in additionalLights)
                {
                    if(additionalLight == null)
                    {
                        additionalLights.Remove(additionalLight);
                    }
                }
                return additionalLights; 
            } 
        }
        private IEnumerable<ReflectionProbe> ReflectionProbes 
        {  
            get 
            { 
                if(reflectionProbes.Count == 0) { return null; }
                foreach(ReflectionProbe reflectionProbe in reflectionProbes)
                {
                    if (reflectionProbe == null)
                    {
                        reflectionProbes.Remove(reflectionProbe);
                    }
                }
                return reflectionProbes; 
            } 
        }
        */
        void Start()
        {
            
        }

        #region Temp

        #endregion



        #region EditorOnly
#if UNITY_EDITOR
        public bool CreateLightData(float _hour,bool _isMoon, Light _sun)
        {
            return lightDataHolder.CreateLightData(CreateLightDataProperties(_hour, _isMoon, _sun));
        }
        private void OnValidate()
        {
            //if(lightDataHolder == null)
            //{
            //    Init();
            //}
        }

        public void Init()
        {
            if (lightDataHolder)
            {
                AssetDatabase.DeleteAsset(lightDataHolder.dataFolderPath);
            }

            string scenePath = SceneManager.GetActiveScene().path.Replace(".unity", "");
            string sceneName = SceneManager.GetActiveScene().name;
            string sceneDataPath = scenePath + "/Resources";
            CheckIfDirectoryExistAndCreate(sceneDataPath);

            lightDataHolder = ScriptableObject.CreateInstance<LightDataHolder>();

            LightDataHolderInitParameters lightDataHolderInitParameters = new LightDataHolderInitParameters
            {
                sceneName = sceneName,
                dataFolderPath = sceneDataPath
            };

            lightDataHolder.Init(lightDataHolderInitParameters);

            AssetDatabase.CreateAsset(lightDataHolder, sceneDataPath + "/" + sceneName + "_LightDataHolder.asset"); //SceneName_LightData
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(lightDataHolder);

        }
        public void BakingDone()
        {
            lightDataHolder.BakingIsDone();
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

        public LightDataProperties CreateLightDataProperties(float _hour, bool _isMoon, Light _sun)
        {
            float hour = _hour;
            bool isMoon = _isMoon;

            Cubemap[] cubemaps = null;
            if (ReflectionProbes != null && ReflectionProbes.Count > 0)
            {
                ReflectionProbe[] reflectionProbes = (ReflectionProbes as List<ReflectionProbe>).ToArray();
                if (reflectionProbes != null)
                {
                    cubemaps = new Cubemap[reflectionProbes.Length];
                    for (int i = 0; i < cubemaps.Length; i++)
                    {
                        cubemaps[i] = reflectionProbes[i].bakedTexture as Cubemap;
                    }
                }
            }

            LightmapData[] lightmapsData = LightmapSettings.lightmaps;
            Texture2D[] lightmaps = new Texture2D[lightmapsData.Length];
            Texture2D[] shadowmaps = new Texture2D[lightmapsData.Length];

            for (int i = 0; i < lightmapsData.Length; i++)
            {
                lightmaps[i] = lightmapsData[i].lightmapColor;
                shadowmaps[i] = lightmapsData[i].shadowMask;
            }


            LightDataProperties lightDataProperties = new LightDataProperties
            {
                hour = hour,
                additionalLights = (AdditionalLights as List<Light>).ToArray(),
                sun = _sun,
                isMoon = isMoon,
                ReflectionProbes = cubemaps,
                dataFolderPath = lightDataHolder.dataFolderPath,
                sceneName = lightDataHolder.sceneName,
                lightmaps = lightmaps,
                shadowMaps = shadowmaps,
                ambientColor = RenderSettings.ambientLight,
                lightProbesValues = LightmapSettings.lightProbes.bakedProbes,
                lightProbesPositions = LightmapSettings.lightProbes.positions
                
            };
            return lightDataProperties;
        }
#endif
        #endregion
    }
}
