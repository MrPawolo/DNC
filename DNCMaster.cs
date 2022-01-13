using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using ML.DNCHandler.Data;
using System.Threading;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ML.DNCHandler
{
    [ExecuteAlways]
    [ExecuteInEditMode]
    public class DNCMaster : MonoBehaviour
    {
        public bool lightmapIndexCorrection = true;
        public bool useSmoothstep;
        public bool useCompute;
        public ComputeShader compute;
        int kernel; 

        public DNCBakeManager bakeManager;
        public bool setHourManual = false;
        [Range(0, 24)]
        public float hour;
        public bool isTimeRunning = false;
        [Range(0.01f,180)]public float dayNightCycleTime = 6;

        [GradientUsage(false)]
        public Gradient ambientDuringDay;
        public Gradient sunColorDuringDay;
        public AnimationCurve sunIntensityDuringDay;
        public Gradient ambientDuringNight;
        public Gradient moonColorDuringNight;
        public AnimationCurve moonIntensityDuringNight;


        [Tooltip("overrall day & night subdivisions will be a double of that value")]
        [Range(3,24)] public int dayVariantsSubdivisions = 4;
        public Transform constantRotation;
        public Transform sunTrans;
        public Transform moonTrans;
        public Light directionalLight;
        [Tooltip("Additional Light which could change over time")]
        public List<Light> additionalLights = new List<Light>();
        [Tooltip("ReflectionProbes which could change over time")]
        public List<ReflectionProbe> reflectionProbes = new List<ReflectionProbe>();


        [Tooltip("If this is true, Per hour settings wont be updated from ''Day Variants Subdivisions'' but now you have full controll over hours settings")]
        public bool customHours;
        public PerHourSettings[] perHoursSettings;

        public bool updateInEdit;

        [Tooltip("load scene setting for this hour index after [ContextMenu(''Set Hour Settings'')]")]
        public int setHourSettingsIndex;

        //-----------------------------
        //--Baking helper Variables----
        //-----------------------------
        #region Baking Variables
        bool baking = false;
        public bool Baking { get { return baking; } }

        string bakingInfo = "Baking info place holder";
        public string BakingInfo {  get { return bakingInfo; } }

        float bakingProgress = 0.69f;
        public float BakingProgress {  get { return bakingProgress; } }

        public event Action onBakeStarted;
        public event Action onBakeEnd;
        IEnumerator bakingCorutine;
        bool partIsBaked;
        #endregion


        //----------------------------------
        //-----Set Baked Data Variables-----
        //----------------------------------
        #region Set Baked Data Variables
        [Space(10)]
        [Header("---Apply Baked Data Variables---")]
        //---Apply Baked data 
        public float[] hours; //prevoius, next, bufor 
        bool usingShadowmap;

        IEnumerator asyncLightDataChanger;
        IEnumerator combineTextures;
        IEnumerator loadData;
        IEnumerator texLoadingCorutine;

        Material lerpMat;
        int _VAL = Shader.PropertyToID("_Val");
        int _FIRST = Shader.PropertyToID("_First");
        int _SECOND = Shader.PropertyToID("_Second");

        //---Old---
        [HideInInspector] public LightProbesHolder[] lightProbesHolders = new LightProbesHolder[3]; // previous, next, buffor, combined
        [HideInInspector] public SphericalHarmonicsL2[] combinedLightProbes;
        [HideInInspector] public DNCTexture2DArray[] lightmaps = new DNCTexture2DArray[4]; //prefvious, next, buffor, combined
        [HideInInspector] public RenderTexture[] combinedRenderLightmaps;
        [HideInInspector] public DNCTexture2DArray[] shadowmasks = new DNCTexture2DArray[4]; //previous, next, buffor, combined
        [HideInInspector] public RenderTexture[] combinedRenderShadowmaps;
        [HideInInspector] public DNCCubemapArray[] cubemaps = new DNCCubemapArray[4]; //previous, next, buffor, combined

        #endregion

        private void Start()
        {
            kernel = compute.FindKernel("CSMain");
            if (lerpMat == null)
            {
                lerpMat = new Material(Shader.Find("Hidden/ML/LerpTex"));
            }

            if (LightmapSettings.lightmaps[0].shadowMask != null)
            {
                usingShadowmap = true;
            }
            else
            {
                usingShadowmap = false;
            }

            if (Application.isPlaying)
            {
                StartCoroutine(InitializeData());
            }
        }

        void Update()
        {
            if ((isTimeRunning && !setHourManual && Application.isPlaying) || (!Application.isPlaying && updateInEdit))
            {
                SetHourAndSunRotation();
            }
        }

        private void SetHourAndSunRotation()
        {
            hour += (1 / dayNightCycleTime) * Time.deltaTime * 0.4f;
            if (hour > 24)
            {
                hour -= 24;
            }

            //constantRotation.rotation *= Quaternion.Euler(0, 0, (1 / dayNightCycleTime * 6) * Time.deltaTime );

            ////sync when 12 or 0
            //if ((hour > 11.9f && hour < 12.1f) || (hour < 23.9 && hour < 0.1))
            //{
            constantRotation.rotation = Quaternion.Euler(constantRotation.rotation.eulerAngles.x, constantRotation.rotation.eulerAngles.y, DNCCustomFunctions.HourToRotation(hour));
            //}

            float z = constantRotation.rotation.eulerAngles.z;
            if ((z > 270 && z <= 360) || (z >= 0 && z < 90))
            {
                directionalLight.transform.rotation = sunTrans.rotation;

                float t = DNCCustomFunctions.Remap(hour, new Vector2(6, 18), new Vector2(0, 1));
                directionalLight.color = sunColorDuringDay.Evaluate(t);
                directionalLight.intensity = sunIntensityDuringDay.Evaluate(t);
            }
            else
            {
                float t = 0;
                if (hour > 12)
                {
                    t = DNCCustomFunctions.Remap(hour, new Vector2(18, 24), new Vector2(0, 0.5f));
                }
                else
                {
                    t = DNCCustomFunctions.Remap(hour, new Vector2(0, 6), new Vector2(0.5f, 1));
                }
                directionalLight.transform.rotation = moonTrans.rotation;
                directionalLight.color = moonColorDuringNight.Evaluate(t);
                directionalLight.intensity = moonIntensityDuringNight.Evaluate(t);
            }
        }

        #region Old Aplying lightmap data
        
        IEnumerator InitializeData()
        {
            float initHour = hour;
            hours = DNCCustomFunctions.FindPrevAndNextAndFutureHour(bakeManager.LightDataHolder.hours, initHour);

            for(int i = 0; i < hours.Length - 1; i++)
            {
                float actHour = hours[i];
                loadData = LoadData(i, actHour);
                StartCoroutine(loadData);
                while (loadData != null) { yield return null; }
            }

            LightmapData[] lightmapDatas = LightmapSettings.lightmaps;

            lightmaps[3].textures = new Texture2D[lightmaps[0].textures.Length];
            combinedRenderLightmaps = new RenderTexture[lightmaps[0].textures.Length];
            for(int i = 0; i < combinedRenderLightmaps.Length; i++)
            {
                lightmaps[3].textures[i] = new Texture2D(lightmaps[0].textures[i].width,
                    lightmaps[0].textures[i].height, TextureFormat.RGBAFloat, true);
                lightmaps[3].textures[i].name = lightmaps[0].textures[i].name;

                combinedRenderLightmaps[i] = new RenderTexture(lightmaps[0].textures[i].width, 
                    lightmaps[0].textures[i].height, 0, RenderTextureFormat.ARGBFloat);
                combinedRenderLightmaps[i].enableRandomWrite = true;
                combinedRenderLightmaps[i].useMipMap = true;
                combinedRenderLightmaps[i].Create();
            }

            if (usingShadowmap)
            {
                shadowmasks[3].textures = new Texture2D[shadowmasks[0].textures.Length];
                combinedRenderShadowmaps = new RenderTexture[shadowmasks[0].textures.Length];
                for (int i = 0; i < combinedRenderLightmaps.Length; i++)
                {
                    shadowmasks[3].textures[i] = new Texture2D(shadowmasks[0].textures[i].width,
                        shadowmasks[0].textures[i].height, TextureFormat.RGBAFloat, true);
                    shadowmasks[3].textures[i].name = shadowmasks[0].textures[i].name;

                    combinedRenderShadowmaps[i] = new RenderTexture(shadowmasks[0].textures[i].width,
                        shadowmasks[0].textures[i].height, 0, RenderTextureFormat.ARGBFloat);
                    combinedRenderShadowmaps[i].enableRandomWrite = true;
                    combinedRenderShadowmaps[i].useMipMap = true;
                    combinedRenderShadowmaps[i].Create();
                }
            }

            combinedLightProbes = new SphericalHarmonicsL2[lightProbesHolders[0].lightProbesValues.Length];

            while (!isTimeRunning) { yield return null; }
            asyncLightDataChanger = AsyncLightDataChanger();
            StartCoroutine(asyncLightDataChanger);
        }
        IEnumerator AsyncLightDataChanger()
        {
            while (true)
            {
                float proggress = (hour - hours[0]) / (hours[1] - hours[0]);
                if(hours[0] > 12 && hours[1] < 12)
                {
                    float newHour = hour < 12 ? 24f + hour : hour;
                    float newHour0 = hours[0];
                    float newHour1 = hours[1] + 24;

                    proggress = (newHour - newHour0) / (newHour1 - newHour0);
                }

                if (proggress >= 1)
                {
                    if (true)
                    {
                        loadData = LoadData(2, hours[2]);
                        StartCoroutine(loadData);
                    }

                    while(loadData != null) { yield return null; }

                    MoveTextures();
                    Resources.UnloadUnusedAssets();
                    hours = DNCCustomFunctions.FindPrevAndNextAndFutureHour(bakeManager.LightDataHolder.hours, hour);
                    proggress = (hour - hours[0]) / (hours[1] - hours[0]);
                }

                /* //Moge castowaæ z renderTexture na Texture a z Textrure na Texture2D Textura mo¿e byæ modyfikowania na innym rdzeniu, trzeba sprawdziæ wydajnoœæ:3
                Texture tex = lightmaps[0].textures[0];
                Texture tex1 = combinedRenderLightmaps[0];
                Texture2D tex2D = tex as Texture2D;
                */

                if (combineTextures == null)
                {
                    combineTextures = CombineTextures(proggress);
                    StartCoroutine(combineTextures);
                }
                while (combineTextures != null) { yield return null; }

                while (!isTimeRunning) { yield return null; }
                //od nowa
            }
        }
        void MoveTextures()
        {
            Array.Copy(lightmaps[1].textures, lightmaps[0].textures, lightmaps[0].textures.Length);
            Array.Copy(lightmaps[2].textures, lightmaps[1].textures, lightmaps[1].textures.Length);
            for(int i = 0; i < lightmaps[2].textures.Length; i++)
            {
                lightmaps[2].textures[i] = null;
            }
            
            Array.Copy(shadowmasks[1].textures, shadowmasks[0].textures, shadowmasks[0].textures.Length);
            Array.Copy(shadowmasks[2].textures, shadowmasks[1].textures, shadowmasks[1].textures.Length);
            for (int i = 0; i < shadowmasks[2].textures.Length; i++)
            {
                shadowmasks[2].textures[i] = null; 
            }

            Array.Copy(cubemaps[1].cubemaps, cubemaps[0].cubemaps, cubemaps[0].cubemaps.Length);
            Array.Copy(cubemaps[2].cubemaps, cubemaps[1].cubemaps, cubemaps[1].cubemaps.Length);
            for (int i = 0; i < cubemaps[2].cubemaps.Length; i++)
            {
                cubemaps[2].cubemaps[i] = null;
            }

            LightProbesHolder probeHolder = lightProbesHolders[1];
            lightProbesHolders[0] = probeHolder;
            probeHolder = lightProbesHolders[2];
            lightProbesHolders[1] = probeHolder;
            lightProbesHolders[2] = null;
        }
        IEnumerator LoadData(int targetTexIndex, float _actHour)
        {
            LightData lightData = bakeManager.GetLightData(_actHour);
            string[] lightmapPaths = lightData.lightDataStorage.lightmapsPath;
            string[] shadowmapPaths = lightData.lightDataStorage.shadowMapsPath;
            string[] reflectionProbesPaths = lightData.lightDataStorage.reflectionProbesPath;
            string lightProbesPath = lightData.lightDataStorage.lightProbesHolderPath;


            //loading Lightmaps
            lightmaps[targetTexIndex].textures = new Texture2D[lightmapPaths.Length];
            texLoadingCorutine = LoadTextures2DFromMemory(lightmapPaths, lightmaps[targetTexIndex].textures);
            StartCoroutine(texLoadingCorutine);
            while (texLoadingCorutine != null) { yield return null; }

            //loading Shadowmaps
            if (usingShadowmap)
            {
                shadowmasks[targetTexIndex].textures = new Texture2D[shadowmapPaths.Length];
                texLoadingCorutine = LoadTextures2DFromMemory(shadowmapPaths, shadowmasks[targetTexIndex].textures);
                StartCoroutine(texLoadingCorutine);
                while (texLoadingCorutine != null) { yield return null; }
            }

            //loading reflectionProbes
            cubemaps[targetTexIndex].cubemaps = new Cubemap[reflectionProbesPaths.Length];
            texLoadingCorutine = LoadCubemapsFromMemory(reflectionProbesPaths, cubemaps[targetTexIndex].cubemaps);
            StartCoroutine(texLoadingCorutine);
            while (texLoadingCorutine != null) { yield return null; }

            //loading LightProbes
            ResourceRequest request = Resources.LoadAsync<LightProbesHolder>(PrepareResourcesPath(lightProbesPath));
            while (!request.isDone) { yield return null; }
            lightProbesHolders[targetTexIndex] = request.asset as LightProbesHolder;

            loadData = null;
        }
        IEnumerator CombineTextures(float _val)
        {
            Thread lighProbesThread = new Thread(() => LerpLightProbes(lightProbesHolders[0].lightProbesValues, 
                lightProbesHolders[1].lightProbesValues, combinedLightProbes, _val) );
            lighProbesThread.Start();

            float t = _val;
            if (useSmoothstep)
            {
                t = Mathf.SmoothStep(0, 1, _val);
            }

            for (int i = 0; i < lightmaps[0].textures.Length; i++)
            {
                LerpTextures(t, lightmaps[0].textures[i], lightmaps[1].textures[i], combinedRenderLightmaps[i]);
                yield return null;
                Graphics.CopyTexture(combinedRenderLightmaps[i], lightmaps[3].textures[i]);
                yield return null;
            }

            if (usingShadowmap)
            {
                for (int i = 0; i < shadowmasks[0].textures.Length; i++)
                {
                    LerpTextures(t, shadowmasks[0].textures[i], shadowmasks[1].textures[i], combinedRenderShadowmaps[i]);
                    yield return null;
                    Graphics.CopyTexture(combinedRenderShadowmaps[i], shadowmasks[3].textures[i]);
                    yield return null;
                }
            }

            while ( lighProbesThread.IsAlive) { yield return null; }

            LightmapData[] lightmapDatas = LightmapSettings.lightmaps;
            if (lightmapIndexCorrection)
            {
                Dictionary<string, int> indexDic = new Dictionary<string, int>();

                for(int i = 0; i < lightmapDatas.Length; i++)
                {
                    indexDic.Add(lightmapDatas[i].lightmapColor.name, i);
                }


                for (int i = 0; i < lightmapDatas.Length; i++)
                {
                    int lightmapIndex = indexDic[lightmaps[3].textures[i].name];

                    lightmapDatas[lightmapIndex].lightmapColor = lightmaps[3].textures[i];
                    if (usingShadowmap)
                    {
                        lightmapDatas[lightmapIndex].shadowMask = shadowmasks[3].textures[i];
                    }
                }
            }
            else
            {
                for (int i = 0; i < lightmapDatas.Length; i++)
                {
                    lightmapDatas[i].lightmapColor = lightmaps[3].textures[i];
                    if (usingShadowmap)
                    {
                        lightmapDatas[i].shadowMask = shadowmasks[3].textures[i];
                    }
                }
            }
            
            LightmapSettings.lightmaps = lightmapDatas;
            LightmapSettings.lightProbes.bakedProbes = combinedLightProbes;

            combineTextures = null;
        }
        private void LerpTextures(float _val, Texture first, Texture second, RenderTexture renderTexture)
        {
            if (!useCompute)
            {
                lerpMat.SetFloat(_VAL, _val);
                lerpMat.SetTexture(_FIRST, first);
                lerpMat.SetTexture(_SECOND, second);

                RenderTexture temp = RenderTexture.GetTemporary(first.width, first.height, 0, RenderTextureFormat.DefaultHDR);
                Graphics.Blit(renderTexture, temp);
                Graphics.Blit(temp, renderTexture, lerpMat);
                RenderTexture.ReleaseTemporary(temp);
            }
            else
            {
                compute.SetFloat(_VAL, _val);
                compute.SetFloat("_WidthHeight", first.width);
                compute.SetTexture(kernel,_FIRST, first);
                compute.SetTexture(kernel,_SECOND, second);
                compute.SetTexture(kernel, "Result", renderTexture);

                compute.Dispatch(kernel, first.width / 8, first.width / 8, 1);
            }
        }
        void LerpLightProbes(SphericalHarmonicsL2[] first, SphericalHarmonicsL2[] second, SphericalHarmonicsL2[] resoult, float _val)
        {
            for(int i = 0; i < first.Length; i++)
            {
                resoult[i] = DNCCustomFunctions.LerpSphericalHarmonics(first[i], second[i], _val);
            }
        }
        IEnumerator LoadTextures2DFromMemory(string[] _paths, Texture2D[] _textures)
        {
            for (int i = 0; i < _paths.Length; i++)
            {
                string path = PrepareResourcesPath(_paths[i]);
                ResourceRequest resourceRequest = Resources.LoadAsync<Texture2D>(path);
                yield return resourceRequest.isDone;
                _textures[i] = resourceRequest.asset as Texture2D;
            }
            texLoadingCorutine = null;
        }
        IEnumerator LoadCubemapsFromMemory(string[] _paths, Cubemap[] _textures)
        {
            for (int i = 0; i < _paths.Length; i++)
            {
                string path = PrepareResourcesPath(_paths[i]);
                ResourceRequest resourceRequest = Resources.LoadAsync<Cubemap>(path);
                yield return resourceRequest.isDone;
                _textures[i] = resourceRequest.asset as Cubemap;
            }
            texLoadingCorutine = null;
        }
        string PrepareResourcesPath(string path)
        {
            string[] sep = { "/Resources/", "." };
            string[] s = path.Split(sep, System.StringSplitOptions.None);

            if (s.Length == 3)
            {
                return s[1];
            }
            else
            {
                Debug.LogError("Invalid path: " + path);
                return "";
            }
        }

        #endregion

        public void LoadHour(float _hour)
        {
            StopAllCoroutines();
            StartCoroutine(InitializeData());
        }

        //--------------------------
        //----ContextMenu Calls-----
        //--------------------------
        #region ContextMenuCalls
        [ContextMenu("Load Data")]
        void LoadData()
        {
            StartCoroutine(LoadData(2, hours[2]));
        }

        [ContextMenu("Set Hour Settings")]
        public void SetHourSettings()
        {
            if (setHourSettingsIndex > perHoursSettings.Length) { return; }
            PerHourSettings sceneSettings = perHoursSettings[setHourSettingsIndex];
            directionalLight.transform.rotation = sceneSettings.sunData.rotation;
            directionalLight.color = sceneSettings.sunData.color;
            directionalLight.intensity = sceneSettings.sunData.intensity;

            for (int i = 0; i < additionalLights.Count; i++)
            {
                additionalLights[i].intensity = sceneSettings.additionalLightsDatas[i].intensity;
                additionalLights[i].color = sceneSettings.additionalLightsDatas[i].color;
            }

            RenderSettings.ambientLight = sceneSettings.ambienColor;
        }
        [ContextMenu("Load Hour (hour)")]
        public void LoadHourContext()
        {
            LoadHour(hour);
        }
        #endregion

        //-----------------------
        //---Editor Only---------
        //-----------------------
        #region EditorOnly
#if UNITY_EDITOR

        public void SetPerHourArray()
        {
            if (!customHours)
            {
                perHoursSettings = new PerHourSettings[dayVariantsSubdivisions * 2];
            }

            Quaternion backup = constantRotation.rotation;

            bakeManager.ReflectionProbes = reflectionProbes;
            bakeManager.AdditionalLights = additionalLights;

            for (int i = 0; i < perHoursSettings.Length; i++)
            {
                if (!customHours)
                {
                    float _hour = 6 + (24 / ((float)dayVariantsSubdivisions * 2)) * i + (24 / ((float)dayVariantsSubdivisions * 4));
                    if (_hour > 24)
                    {
                        _hour -= 24;
                    }
                    perHoursSettings[i].hour = _hour;
                }
                float hour = perHoursSettings[i].hour;

                constantRotation.rotation = Quaternion.Euler(constantRotation.rotation.eulerAngles.x, 
                    constantRotation.rotation.eulerAngles.y, 
                    DNCCustomFunctions.HourToRotation(hour));

                if (hour > 6 && hour < 18)
                {
                    float t = DNCCustomFunctions.Remap(hour, new Vector2(6, 18), new Vector2(0, 1));
                    perHoursSettings[i].sunData.rotation = sunTrans.rotation;
                    perHoursSettings[i].sunData.isMoon = false;
                    perHoursSettings[i].sunData.color = sunColorDuringDay.Evaluate(t);
                    perHoursSettings[i].sunData.intensity = sunIntensityDuringDay.Evaluate(t);
                    perHoursSettings[i].ambienColor = ambientDuringDay.Evaluate(t);
                }
                else
                {
                    float t;
                    if(18 < hour && hour < 24)
                    {
                        t = DNCCustomFunctions.Remap(hour, new Vector2(18, 24), new Vector2(0, 0.5f));
                    }
                    else
                    {
                        t = DNCCustomFunctions.Remap(hour, new Vector2(0, 6), new Vector2(0.5f, 1));
                    }
                    
                    perHoursSettings[i].sunData.rotation = moonTrans.rotation;
                    perHoursSettings[i].sunData.isMoon = true;
                    perHoursSettings[i].sunData.color = moonColorDuringNight.Evaluate(t);
                    perHoursSettings[i].sunData.intensity = moonIntensityDuringNight.Evaluate(t);
                    perHoursSettings[i].ambienColor = ambientDuringNight.Evaluate(t);
                }


            }
            constantRotation.rotation = backup;
        }
        

        public void StartBakingLightData()
        {
            if(bakingCorutine == null)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                bakingCorutine = BakeLightData();
                StartCoroutine(bakingCorutine);
            }
        }

        void SetSceneToBake(int index)
        {
            PerHourSettings sceneSettings = perHoursSettings[index];
            directionalLight.transform.rotation = sceneSettings.sunData.rotation;
            directionalLight.color = sceneSettings.sunData.color;
            directionalLight.intensity = sceneSettings.sunData.intensity;

            for(int i = 0; i < additionalLights.Count; i++)
            {
                additionalLights[i].intensity = sceneSettings.additionalLightsDatas[i].intensity;
                additionalLights[i].color = sceneSettings.additionalLightsDatas[i].color;
            }

            RenderSettings.ambientLight = sceneSettings.ambienColor;
        }
        public IEnumerator BakeLightData()
        {
            baking = true;
            Debug.Log("Baking Start");
            onBakeStarted?.Invoke();
            bakeManager.Init();
            
            int bakingScenarious = dayVariantsSubdivisions * 2;
            for(int i = 0; i < bakingScenarious; i++)
            {
                bakingInfo = "Baking LightData: " + (i + 1).ToString() + "/" + (bakingScenarious).ToString();
                bakingProgress = (float)i / (float)bakingScenarious;

                //set scene properties acordingly to time
                SetSceneToBake(i);

                //bake 
                Lightmapping.BakeAsync();


                //bakingPart = true;
                //yield return StartCoroutine(BakeCompleatedLightmappingEvent());

                bool baked = false;
                Action action = () => baked = true;
                Lightmapping.bakeCompleted += action.Invoke;
                yield return new WaitUntil(() => baked);
                Lightmapping.bakeCompleted -= action.Invoke;

                //if (Lightmapping.isRunning )
                //{
                //    yield return null;
                //}


                bakeManager.CreateLightData(perHoursSettings[i].hour, perHoursSettings[i].sunData.isMoon, directionalLight);
                //save resoults
            }

            bakeManager.BakingDone();
            onBakeEnd?.Invoke();
            baking = false;
            bakingCorutine = null;
            Debug.Log("Baking Light Data was successful");
            EditorUtility.ClearProgressBar();
        }
        public void StopBakingLightData()
        {
            baking = false;
            if(bakingCorutine != null)
            {
                StopCoroutine(bakingCorutine);
                onBakeEnd?.Invoke();
                bakingCorutine = null;
                EditorUtility.ClearProgressBar();
            }
        }
        public void OnValidate()
        {
            for (int i = 0; i < perHoursSettings.Length; i++)
            {
                PerHourSettings perHourSettings = perHoursSettings[i];
                //perHourSettings.name = DNCCustomFunctions.FloatHourToStandard(perHourSettings.hour).ToString();
                perHourSettings.name = perHourSettings.hour.ToString();
                perHoursSettings[i] = perHourSettings;
            }
            if (!bakeManager)
            {
                bakeManager = GetComponent<DNCBakeManager>();
                if (!bakeManager)
                {
                    bakeManager = gameObject.AddComponent<DNCBakeManager>();
                }
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) //This function cheat to make continous update corutine in editor
            {
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
#endif
    #endregion
    }


    #region CustomEditor
#if UNITY_EDITOR
    [CustomEditor(typeof(DNCMaster))]
    public class DNCMasterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DNCMaster master = (DNCMaster)target;

            
            if (GUILayout.Button("Set Per Hour Array") && !master.Baking)
            {
                master.SetPerHourArray();
                master.OnValidate();
            }
            if (GUILayout.Button("Bake LightData"))
            {
                if (EditorUtility.DisplayDialog("Bake LightData", "Are you sure you want to bake LightData, this will remove old baked LightData", "Bake!", "NO"))
                {
                    master.StartBakingLightData();
                }
            }
            if (master.Baking)
            {
                if(EditorUtility.DisplayCancelableProgressBar("Baking LightData", master.BakingInfo, master.BakingProgress))
                {
                    master.StopBakingLightData();
                    EditorUtility.ClearProgressBar();
                }
            }
        }
        
    }
#endif
    #endregion
}
