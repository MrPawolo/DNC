using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ML.DNCHandler
{
    [RequireComponent(typeof(Light))]
    public class ColorTemp : MonoBehaviour
    {
        [Range(1800,7000)] public float colorTemp  = 4000;
        Light lightSource;

        public void Start()
        {
            GetAndSetColor();
        }
        private void OnValidate()
        {
            GetAndSetColor();
        }

        void GetAndSetColor()
        {
            lightSource = GetComponent<Light>();
            lightSource.useColorTemperature = true;
            lightSource.colorTemperature = colorTemp;
        }
    }
}
