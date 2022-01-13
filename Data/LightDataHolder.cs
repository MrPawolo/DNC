using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace ML.DNCHandler.Data
{
    public struct LightDataHolderInitParameters
    {
        public string dataFolderPath;
        public string sceneName;
    }

    public class LightDataHolder : ScriptableObject
    {
        public Dictionary<float,LightData> lightDatas = new Dictionary<float, LightData>();
        public List<LightData> lightDataInspector = new List<LightData>();
        public float[] hours;
        public string dataFolderPath;
        public string sceneName;

        public LightData GetLightData(float _hour)
        {
            if(!lightDatas.ContainsKey(_hour)) { Debug.LogError("No key was found, key: " + _hour.ToString()); return null; }

            return lightDatas[_hour];
        }

        private void Awake()
        {
            SetDictionary();
            SetHours();
        }
        private void OnValidate()
        {
            SetDictionary();
            SetHours();
        }
        public void SetDictionary()
        {
            lightDatas.Clear();
            foreach (LightData lightData in lightDataInspector)
            {
                lightDatas.Add(lightData.lightDataStorage.hour, lightData);
            }
        }
        public void SetHours()
        {
            hours = new float[lightDataInspector.Count];
            for(int i = 0; i < hours.Length; i++)
            {
                hours[i] = lightDataInspector[i].lightDataStorage.hour;
            }
            System.Array.Sort<float>(hours);
        }
        //----------------------------
        //--------Editor Only---------
        //----------------------------
        #region EditorOnly
#if UNITY_EDITOR

        public void Init(LightDataHolderInitParameters initParameters)
        {
            dataFolderPath = initParameters.dataFolderPath;
            sceneName = initParameters.sceneName;
        }


        /// <summary>
        /// Create LightData, return true if creating was succesfull
        /// </summary>
        /// <param name="_lightDataProperties"></param>
        /// <returns></returns>
        public bool CreateLightData(LightDataProperties _lightDataProperties)
        {
            Debug.Log("CreatingLightData");
            if (!IsPathsValid())
            {
                return false;
            } 



            LightData createdLightData = ScriptableObject.CreateInstance<LightData>();
            bool isDone = createdLightData.Init(_lightDataProperties);
            lightDataInspector.Add(createdLightData);

            string newHour = _lightDataProperties.hour.ToString().Replace(".", ",");
            AssetDatabase.CreateAsset(createdLightData, dataFolderPath + "/" + sceneName + "_" +  "LightData_" + newHour + ".asset"); //SceneName_LightData_hour
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);

            if(isDone == false)
            {
                Debug.Log("Creating LightingData.Init Failed");
            }

            return isDone;
        }

        public void BakingIsDone()
        {

        }

        

        

        public bool IsPathsValid()
        {
            if (string.IsNullOrEmpty(dataFolderPath))
            {
                Debug.LogError("Data folder path isnt Valid");
                return false;
            }
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("Scene name isnt Valid");
                return false;
            }
            return true;
        }

#endif
        #endregion
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(LightDataHolder))]
    public class LightDataHolderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LightDataHolder lightDataHolder = (LightDataHolder)target;


            GUILayout.Label("lightDatas count: " + lightDataHolder.lightDatas.Count.ToString());

            if(GUILayout.Button("Set LightDatas"))
            {
                lightDataHolder.SetDictionary();
            }

        }
    }

#endif
}
