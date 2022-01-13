using UnityEngine;
using UnityEngine.Rendering;

namespace ML.DNCHandler.Data
{
    public class LightProbesHolder : ScriptableObject
    {
        public SphericalHarmonicsL2[] lightProbesValues;
        public Vector3[] lightProbesPositions;
    }
}
