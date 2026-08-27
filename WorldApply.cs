using DV.Simulation.Brake;
using DV.Utils;
using UnityEngine;

namespace SelfLappingBrakes
{
    static class WorldApply
    {
        public static void ApplyAll()
        {
            try
            {
                var spawner = SingletonBehaviour<CarSpawner>.Instance;
                if (spawner?.AllLocos == null)
                {
                    return;
                }

                foreach (var loco in spawner.AllLocos)
                {
                    ApplyLoco(loco);
                }
            }
            catch
            {
            }
        }

        public static void ApplyBrakeSystem(BrakeSystem brakes)
        {
            if (brakes == null)
            {
                return;
            }

            if (Target.Applies(Target.CarFrom(brakes)))
            {
                brakes.selfLappingController = true;
                CurveCopy.Apply(brakes);
            }
        }

        public static void ApplyLoco(TrainCar? loco)
        {
            if (loco == null || loco.brakeSystem == null)
            {
                return;
            }

            if (!Target.Applies(loco))
            {
                return;
            }

            loco.brakeSystem.selfLappingController = true;
            CurveCopy.Apply(loco.brakeSystem);

            if (loco.IsInteriorLoaded && loco.loadedInterior != null)
            {
                var mgr = loco.loadedInterior.GetComponentInChildren<DV.HUD.InteriorControlsManager>(true);
                if (mgr != null &&
                    mgr.TryGetControl(DV.HUD.InteriorControlsManager.ControlType.TrainBrake, out var reference) &&
                    reference.controlImplBase != null)
                {
                    LeverConvert.Apply(reference.controlImplBase.gameObject);
                }
            }
        }
    }
}
