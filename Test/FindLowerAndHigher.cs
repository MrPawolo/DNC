using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ML.DNCHandler
{
    public class FindLowerAndHigher : MonoBehaviour
    {
        public float[] numbers;
        public float searchNumer;
        public Vector2 randomRange;
        public int lenght = 10;

        [Range(-180, 180)]
        public int rot;
        public float retHour;
        public float retRot;
        public bool doingNothing;

        private void OnValidate()
        {
            retHour = DNCCustomFunctions.RotationToHour(rot);
            retRot = DNCCustomFunctions.HourToRotation(retHour);
        }

        [ContextMenu("Generate Random Numbers")]
        public void GenRandNumbers()
        {
            Vector2 range;
            if(Vector2.zero == randomRange)
            {
                range = new Vector2(-100, 100);
            }
            else
            {
                range = randomRange;
            }
            numbers = new float[lenght];
            for(int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = Mathf.Round(Random.Range(range.x, range.y));
            }
            System.Array.Sort<float>(numbers);
        }

        [ContextMenu("Find")]
        public void Find()
        {
            //float lowest = randomRange.y;
            //float higest = randomRange.x;

            //float lower = randomRange.x;
            //float higher = randomRange.y;
            //for(int i = 0; i < numbers.Length; i++)
            //{
            //    if(numbers[i] < searchNumer)
            //    {
            //        if(numbers[i] > lower)
            //        {
            //            lower = numbers[i];
            //        }
            //        if (lowest > numbers[i])
            //        {
            //            lowest = numbers[i];
            //        }
            //    }
            //    else if(numbers[i] > searchNumer)
            //    {
            //        if(numbers[i] < higher)
            //        {
            //            higher = numbers[i];
            //        }
            //        if(higest < numbers[i])
            //        {
            //            higest = numbers[i];
            //        }
            //    }
            //}
            //if(searchNumer > higest)
            //{
            //    higher = lowest;
            //}
            //if (searchNumer < lowest)
            //{
            //    lower = higest;
            //}
            //Debug.Log($"Ref num: {searchNumer} ,hiher: {higher} ,lower: {lower} ,highest: {higest} ,lowest: {lowest}");

            float[] num = DNCCustomFunctions.FindPrevAndNextAndFutureHour(numbers, searchNumer);
            foreach (float val in num)
            {
                Debug.Log(val);
            }
        }
    }
}
