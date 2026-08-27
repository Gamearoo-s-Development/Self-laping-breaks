using DV.CabControls;
using DV.HUD;
using DV.Simulation.Brake;
using DV.Simulation.Controllers;
using HarmonyLib;
using UnityEngine;

namespace SelfLappingBrakes
{
    [HarmonyPatch(typeof(BrakeSystem), nameof(BrakeSystem.Initialize))]
    static class BrakeSystemInitPatch
    {
        static void Prefix(BrakeSystem __instance, ref bool selfLappingController)
        {
            if (Target.Applies(Target.CarFrom(__instance)))
            {
                selfLappingController = true;
            }
        }

        static void Postfix(BrakeSystem __instance)
        {
            WorldApply.ApplyBrakeSystem(__instance);
        }
    }

    [HarmonyPatch(typeof(BrakeSystem), "SimulateTrainBrake")]
    static class SimulateTrainBrakePatch
    {
        static void Prefix(BrakeSystem __instance)
        {
            if (!Target.Applies(Target.CarFrom(__instance)))
            {
                return;
            }

            __instance.selfLappingController = true;
            CurveCopy.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(TrainCar), "Start")]
    static class TrainCarStartPatch
    {
        static void Postfix(TrainCar __instance)
        {
            WorldApply.ApplyLoco(__instance);
        }
    }

    [HarmonyPatch(typeof(LeverBase), "Initialize")]
    static class LeverInitPatch
    {
        static void Prefix(LeverBase __instance)
        {
            if (!Target.Applies(Target.CarFrom(__instance)) ||
                !LeverConvert.IsTrainBrake(__instance.gameObject))
            {
                return;
            }

            LeverConvert.ApplySpec(__instance.gameObject, LeverConvert.NotchCount(__instance.gameObject));
        }
    }

    [HarmonyPatch(typeof(SteppedJoint), "Start")]
    static class SteppedJointStartPatch
    {
        static void Prefix(SteppedJoint __instance)
        {
            if (!Target.Applies(Target.CarFrom(__instance)) ||
                !LeverConvert.IsTrainBrake(__instance.gameObject))
            {
                return;
            }

            var count = LeverConvert.NotchCount(__instance.gameObject);
            LeverConvert.ApplySpec(__instance.gameObject, count);
            LeverConvert.PrepareStepped(__instance, count);
            NotchFeel.ApplyLive(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(InteriorControlsManager), "SetupControl")]
    static class SetupControlPatch
    {
        static void Postfix(InteriorControlsManager __instance, GameObject reference, InteriorControlsManager.ControlType type)
        {
            if (type != InteriorControlsManager.ControlType.TrainBrake ||
                reference == null ||
                !Target.Applies(__instance.Car))
            {
                return;
            }

            LeverConvert.Apply(reference);
        }
    }

    [HarmonyPatch(typeof(BrakeControl), nameof(BrakeControl.Init))]
    static class BrakeControlInitPatch
    {
        static void Postfix(BrakeControl __instance)
        {
            var car = __instance.GetComponentInParent<TrainCar>();
            if (!Target.Applies(car))
            {
                return;
            }

            LeverConvert.ApplyControl(__instance, LeverConvert.DefaultNotches);
        }
    }
}
