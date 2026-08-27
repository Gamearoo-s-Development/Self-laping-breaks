using System;
using DV.CabControls;
using DV.CabControls.Spec;
using DV.HUD;
using DV.Simulation.Controllers;
using HarmonyLib;
using UnityEngine;

namespace SelfLappingBrakes
{
    static class LeverConvert
    {
        public const int DefaultNotches = 12;

        public static bool IsTrainBrake(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            var mgr = go.GetComponentInParent<InteriorControlsManager>();
            if (mgr != null &&
                mgr.TryGetControl(InteriorControlsManager.ControlType.TrainBrake, out var reference) &&
                reference.controlImplBase != null)
            {
                var ctrl = reference.controlImplBase.transform;
                if (go.transform == ctrl || go.transform.IsChildOf(ctrl) || ctrl.IsChildOf(go.transform))
                {
                    return true;
                }
            }

            for (var t = go.transform; t != null; t = t.parent)
            {
                if (NameLooksLikeTrainBrake(t.name))
                {
                    return true;
                }
            }

            return false;
        }

        static bool NameLooksLikeTrainBrake(string name)
        {
            return name.IndexOf("TrainBrake", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("BrakeTrain", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("train brake", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("TRN", StringComparison.OrdinalIgnoreCase) >= 0 &&
                      name.IndexOf("BRK", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static int NotchCount(GameObject go)
        {
            var root = go.GetComponentInParent<LeverBase>()?.gameObject ?? go;
            var customField = AccessTools.Field(typeof(NotchedControlCustomNames), "notches");
            var namesField = AccessTools.Field(typeof(NotchedControlCustomNames), "customNames");
            foreach (var named in root.GetComponentsInChildren<NotchedControlCustomNames>(true))
            {
                if (customField?.GetValue(named) is int n && n >= 8)
                {
                    return n;
                }

                if (namesField?.GetValue(named) is string[] names && names.Length >= 8)
                {
                    return names.Length;
                }
            }

            var numberedField = AccessTools.Field(typeof(NotchedControlNumberedNames), "notches");
            foreach (var numbered in root.GetComponentsInChildren<NotchedControlNumberedNames>(true))
            {
                if (numberedField?.GetValue(numbered) is int n && n >= 8)
                {
                    return n;
                }
            }

            return DefaultNotches;
        }

        public static void Apply(GameObject? go)
        {
            if (go == null)
            {
                return;
            }

            var count = NotchCount(go);
            ApplySpec(go, count);
            var lever = go.GetComponent<LeverBase>() ?? go.GetComponentInChildren<LeverBase>(true);
            ApplyStepped(lever, count);
            ApplyIndicator(go, count);
            NotchFeel.ApplyLive(go);
        }

        public static void ApplySpec(GameObject go, int count)
        {
            var lever = go.GetComponentInChildren<Lever>(true);
            if (lever != null)
            {
                lever.useSteppedJoint = true;
                lever.steppedValueUpdate = true;
                lever.notches = count;
                lever.useInnerLimitSpring = false;
                lever.innerLimitMinNotch = 0;
                lever.innerLimitMaxNotch = count - 1;
            }

            var rotary = go.GetComponentInChildren<Rotary>(true);
            if (rotary != null)
            {
                rotary.useSteppedJoint = true;
                rotary.notches = count;
                rotary.useInnerLimitSpring = false;
                rotary.innerLimitMinNotch = 0;
                rotary.innerLimitMaxNotch = count - 1;
            }

            NotchFeel.ApplySpec(go);
        }

        public static void PrepareStepped(SteppedJoint stepped, int count)
        {
            if (stepped == null)
            {
                return;
            }

            stepped.notches = count;
            stepped.useInnerLimitSpring = false;
            stepped.innerLimitMinNotch = 0;
            stepped.innerLimitMaxNotch = count - 1;
            stepped.isSpringActive = true;
        }

        public static void ApplyStepped(LeverBase? lever, int count)
        {
            if (lever == null)
            {
                return;
            }

            var stepped = AccessTools.Field(typeof(LeverBase), "steppedJoint")?.GetValue(lever) as SteppedJoint;
            if (stepped == null)
            {
                return;
            }

            stepped.enabled = true;
            PrepareStepped(stepped, count);

            var rangeObj = AccessTools.Field(typeof(SteppedJoint), "angleRange")?.GetValue(stepped);
            if (rangeObj is float range && range > 0.01f && count > 1)
            {
                AccessTools.Field(typeof(SteppedJoint), "<SingleNotchAngle>k__BackingField")
                    ?.SetValue(stepped, range / (count - 1));
            }
        }

        public static void ApplyIndicator(GameObject go, int count)
        {
            foreach (var named in go.GetComponentsInChildren<NotchedControlCustomNames>(true))
            {
                AccessTools.Field(typeof(NotchedControlCustomNames), "notches")?.SetValue(named, count);
            }

            foreach (var numbered in go.GetComponentsInChildren<NotchedControlNumberedNames>(true))
            {
                AccessTools.Field(typeof(NotchedControlNumberedNames), "notches")?.SetValue(numbered, count);
            }
        }

        public static void ApplyControl(OverridableBaseControl control, int count)
        {
            if (control == null)
            {
                return;
            }

            AccessTools.PropertySetter(typeof(OverridableBaseControl), "IsNotched")
                ?.Invoke(control, new object[] { true });
            AccessTools.PropertySetter(typeof(OverridableBaseControl), "NotchCount")
                ?.Invoke(control, new object[] { (float)count });
        }
    }
}
