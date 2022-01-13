
using UnityEngine;
using UnityEngine.Rendering;


namespace ML.DNCHandler
{
    public struct DNCCustomFunctions
    {
        /// <summary>
        /// Example: Change 7.5 float to 7.3 float
        /// </summary>
        /// <param name="_hour"></param>
        /// <returns></returns>
        public static float FloatHourToStandard(float _hour)
        {
            float hour = Mathf.Floor(_hour);
            float decimalTime = _hour - hour;
            float minutes = Mathf.Lerp(0, 0.6f, decimalTime);

            return hour + minutes;
        }
        public static float Remap(float In, Vector2 InMinMax, Vector2 OutMinMax)
        {
            return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }


        //      ___0___
        //     /   12  \
        // -90 |6    18| 90
        //     \__24___/
        //     -180 180
        //
        // return the hour when we have the rotation of object
        // The component constains 3 objects:
        //  - Constant rotation which is a partent of:
        //      - sun 
        //      - moon
        // Constant rotation look at the north and in neutral position i has sun looking downwarts at north,
        // and moon looking upwards at north, neutral positon is when Contant rotation has 0 rotation at Z axis,
        // 
        public static float RotationToHour(float rot)
        {
            return Remap(rot, new Vector2(-180, 180), new Vector2(0, 24));
        }

        public static float HourToRotation(float hour)
        {
            return Remap(hour, new Vector2(0, 24), new Vector2(-180, 180));
        }

        /// <summary>
        /// range(low, high)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="numbers"></param>
        /// <param name="searchNumber"></param>
        /// <returns></returns>
        public static float[] FindPrevAndNextAndFutureHour(float[] numbers, float searchNumber)
        {
            float[] resoult = new float[3];
            bool foundPrev = false;

            if(searchNumber > 24)
            {
                searchNumber -= 24;
            }

            for(int i = 0; i < numbers.Length; i++)
            {
                if(numbers[i] <= searchNumber)
                {
                    resoult[0] = i;
                    foundPrev = true;
                }
            }

            if (foundPrev)
            {
                int first = (int)resoult[0];
                int second = first + 1;
                int third = first + 2;

                if(second >= numbers.Length)
                {
                    second -= numbers.Length;
                }
                if(third >= numbers.Length)
                {
                    third -= numbers.Length;
                }
                resoult[0] = numbers[first];
                resoult[1] = numbers[second];
                resoult[2] = numbers[third];
                //resoult = new Vector3(numbers[first], numbers[second], numbers[third]);
            }
            else
            {
                resoult[0] = numbers[numbers.Length - 1];
                resoult[1] = numbers[0];
                resoult[2] = numbers[1];
                //resoult = new Vector3(numbers[numbers.Length - 1],numbers[0], numbers[1]);
            }

            return resoult;
        }

        public static SphericalHarmonicsL2 LerpSphericalHarmonics(SphericalHarmonicsL2 a, SphericalHarmonicsL2 b, float t)
        {
            return (1 - t) * a + b * t;
        }
    }
}
