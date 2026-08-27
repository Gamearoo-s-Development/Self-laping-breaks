using DV.Simulation.Brake;
using UnityEngine;

namespace SelfLappingBrakes
{
    static class CurveCopy
    {
        static AnimationCurve? linear;

        public static void Apply(BrakeSystem brakes)
        {
            if (brakes == null)
            {
                return;
            }

            if (linear == null)
            {
                linear = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            brakes.trainBrakeCurve = linear;
        }
    }
}
