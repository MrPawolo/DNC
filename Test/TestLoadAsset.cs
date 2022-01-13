using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ML.DNCHandler.Data;

namespace ML.DNCHandler
{
    public class TestLoadAsset : MonoBehaviour
    {
        public LightProbesHolder texture;
        public string TextureToLoad;
        public string name;

        public string resourcesPath;
        void Start()
        {
            

            
        }
        [ContextMenu("Load")]
        public void LoadAsset()
        {
            string unityProjectPathBase = Application.dataPath.Replace("Assets", "");
            string[] sep = { "/Resources/", "." };
            string[] s = resourcesPath.Split(sep, System.StringSplitOptions.None);

            if (s.Length == 3)
            {
                Debug.Log(s[1]);
                texture = Resources.Load<LightProbesHolder>(s[1]);
            }

            //string unityProjectPathBase = Application.dataPath.Replace("Assets", "");
            //var bundle = AssetBundle.LoadFromFile(unityProjectPathBase + TextureToLoad);
            //Texture2D tex = bundle.LoadAsset(name) as Texture2D;
            //bundle.Unload(false);
        }
        [ContextMenu("Unload")]
        public void UnloadAsset()
        {
            Resources.UnloadAsset(texture);
        }

    }
}
