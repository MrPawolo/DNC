using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ML.DNCHandler
{
    [System.Serializable]
    public struct SceneLightmapData
    {
        public string name; //this is used only for display list element name as scene name
        public Scene scene;
        public int[] lightMapIndexes; //TODO: This variable could change after unloading scene, so i dont know if it need to be stored
        public string[] lightMapNames;
        //public string[] shadowmasksNames;
        //public List<Renderer> staticRenderers; //TOOD: need to be implemented
    }
    public class SceneLightmapTracker : MonoBehaviour
    {
        


        public List<SceneLightmapData> sceneLightmapDatas = new List<SceneLightmapData>(); //this is needed because dictionary dot have index
        public Dictionary<Scene, SceneLightmapData> sceneLightmapDatasLookUp = new Dictionary<Scene, SceneLightmapData>();

        LightmapData[] previousLightmapData;

        private void Awake()
        {
            previousLightmapData = LightmapSettings.lightmaps;
        }
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            SceneManager.sceneUnloaded -= OnSceneUnload;
        }
        /// <summary>
        /// Copy dictionary of a list and return dictionary, note that old previous data in dictionary will be removed
        /// </summary>
        /// <param name="dictonary"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        Dictionary<Scene, SceneLightmapData> CreateLightmapLookup(Dictionary<Scene, SceneLightmapData> dictonary, List<SceneLightmapData> list)
        {
            dictonary.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                dictonary.Add(list[i].scene, list[i]);
            }
            return dictonary;
        }
        void RebuildLightmapIndexes()
        {
            previousLightmapData = LightmapSettings.lightmaps;
            for (int i = 0, sceneIndexCount = 0; i < sceneLightmapDatas.Count; i++)
            {
                SceneLightmapData sceneLightmapData = sceneLightmapDatas[i];
                for(int j = 0; j < sceneLightmapData.lightMapIndexes.Length; j++)
                {
                    sceneLightmapData.lightMapIndexes[j] = sceneIndexCount;
                    sceneIndexCount++;
                }
                sceneLightmapDatas[i] = sceneLightmapData;
            }
        }
        void OnSceneLoad(Scene _scene, LoadSceneMode _loadSceneMode)
        {
            LightmapData[] newLightmapData = LightmapSettings.lightmaps;
            int lightmapsLenghtDelta = newLightmapData.Length - previousLightmapData.Length; //this is used only to make for loop on new loaded lightmaps
            //previousLightmapData = newLightmapData;

            if (lightmapsLenghtDelta == 0) return; //this mean that the loaded scene dont have baked light

            List<string> lightmapNames = new List<string>();
            int[] lightmapIndexes = new int[lightmapsLenghtDelta];

            int baseIndex = previousLightmapData.Length;
            baseIndex = baseIndex < 0 ? 0 : baseIndex;
            for (int i = 0, j = 0; i < lightmapsLenghtDelta; i++)
            {
                lightmapNames.Add(newLightmapData[baseIndex + i].lightmapColor.name);
                lightmapNames.Add(newLightmapData[baseIndex + i].shadowMask.name);
            }

            SceneLightmapData newScene = new SceneLightmapData
            {
                name = _scene.name,
                scene = _scene,
                lightMapNames = lightmapNames.ToArray(),
                //shadowmasksNames = shadowmasksNames,
                lightMapIndexes = lightmapIndexes
            };
            sceneLightmapDatas.Add(newScene);
            CreateLightmapLookup(sceneLightmapDatasLookUp, sceneLightmapDatas);
            RebuildLightmapIndexes();
        }
        void OnSceneUnload(Scene _scene)
        {
            if (sceneLightmapDatasLookUp.ContainsKey(_scene))
            {
                if (previousLightmapData.Length == LightmapSettings.lightmaps.Length)
                {
                    List<LightmapData> lightmaps = new List<LightmapData>();
                    LightmapData[] oldLightmapData = LightmapSettings.lightmaps;
                    lightmaps.AddRange(oldLightmapData);
                    
                    SceneLightmapData removedSceneLightmapData = sceneLightmapDatasLookUp[_scene]; //unloaded scene lightmap data

                    int[] indexesToRemove = removedSceneLightmapData.lightMapIndexes;
                    for (int i = 0; i < indexesToRemove.Length; i++)
                    {
                        lightmaps.RemoveAt(indexesToRemove[i]);
                    }
                    LightmapData[] correctedLightmapData = lightmaps.ToArray();
                    LightmapSettings.lightmaps = correctedLightmapData;


                    //int removedSceneIndex = sceneLightmapDatas.FindIndex(x => x.Equals(sceneLightmapData)); //
                    int removedSceneIndex = sceneLightmapDatas.IndexOf(removedSceneLightmapData); //
                    int iteracitons = sceneLightmapDatas.Count - removedSceneIndex - 1;
                    Debug.Log(removedSceneIndex.ToString() + " " + iteracitons.ToString());

                    for(int i = 0; i < iteracitons; i++)
                    {
                        int newIndex = removedSceneIndex + 1 +i; //Dla scen o tym idexie trzeba znaleŸæ statyczne obiekty
                        Scene scene = sceneLightmapDatas[newIndex].scene;
                        GameObject[] rootGOs = scene.GetRootGameObjects();
                        Debug.Log(rootGOs.Length);

                        for(int j = 0; j < rootGOs.Length; j++)
                        {
                            MeshRenderer[] renderers = rootGOs[j].GetComponentsInChildren<MeshRenderer>(true);
                            Debug.Log(renderers.Length);
                            for(int k = 0; k < renderers.Length; k++)
                            {
                                if (renderers[k].lightmapIndex != -1)
                                {
                                    renderers[k].lightmapIndex -= indexesToRemove.Length;
                                    
                                    //TODO: This code may need optimalization because it complication grows exponentially 
                                    //potencial solution is to make some kind of root object hold all gameobjects with lightmaps
                                }
                            }
                        }

                    }

                    //znam iloœæ indexów do odjêcia 
                    //trzeba znaleŸæ wszystkie statyczne obiekty co s¹ ponad oczytan¹ scena t¹ scen¹ 
                    
                    //TODO: There i need to manually change static renderes lightmap indexes, but somehow i need to knwo what old index is because of that i can remove scene form the middle 
                    //TODO: Event about manual chenged renderers(propably this because i cant remember by ideas xD)
                }
                SceneLightmapData tempSceneLightmapData = sceneLightmapDatasLookUp[_scene];
                sceneLightmapDatasLookUp.Remove(_scene);
                sceneLightmapDatas.Remove(tempSceneLightmapData);
                RebuildLightmapIndexes();

                //TODO: Event dla DNC Master ¿e lightmapy i ich indexy zosta³y ju¿ przebudowane
            }
            
        }
    }
}
