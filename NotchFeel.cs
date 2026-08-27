using System;
using System.Collections.Generic;
using DV.CabControls;
using DV.CabControls.Spec;
using DV.HUD;
using DV.ThingTypes;
using DV.Utils;
using HarmonyLib;
using UnityEngine;

namespace SelfLappingBrakes
{
    static class NotchFeel
    {
        public const float FallbackSpring = 28f;
        public const float FallbackDamper = 2f;

        static readonly HashSet<int> wiredAudio = new HashSet<int>();

        public static void ApplySpec(GameObject go)
        {
            Resolve(go, out var spring, out var damper, out var clip);

            var lever = go.GetComponentInChildren<Lever>(true);
            if (lever != null)
            {
                lever.useSpring = true;
                lever.jointSpring = spring;
                lever.jointDamper = damper;
                lever.limitVibration = true;
                if (lever.notch == null && clip != null)
                {
                    lever.notch = clip;
                }
            }

            var rotary = go.GetComponentInChildren<Rotary>(true);
            if (rotary != null)
            {
                rotary.useSpring = true;
                rotary.jointSpring = spring;
                rotary.jointDamper = damper;
                if (rotary.notch == null && clip != null)
                {
                    rotary.notch = clip;
                }
            }
        }

        public static void ApplyLive(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            Resolve(go, out var spring, out var damper, out var clip);

            foreach (var joint in go.GetComponentsInChildren<HingeJoint>(true))
            {
                var s = joint.spring;
                s.spring = spring;
                s.damper = damper;
                joint.spring = s;
                joint.useSpring = true;
            }

            foreach (var stepped in go.GetComponentsInChildren<SteppedJoint>(true))
            {
                stepped.isSpringActive = true;
                WireAudio(go, stepped, clip);
            }
        }

        static void WireAudio(GameObject go, SteppedJoint stepped, AudioClip? clip)
        {
            foreach (var audio in go.GetComponentsInChildren<LeverAudio>(true))
            {
                if (audio.notchClip == null && clip != null)
                {
                    audio.notchClip = clip;
                }

                audio.hitVibration = true;
                EnsureSourceAndSubscribe(audio, typeof(LeverAudio), "notchSound", "PlayNotchSound", audio.notchClip ?? clip, stepped);
            }

            foreach (var audio in go.GetComponentsInChildren<RotaryAudio>(true))
            {
                if (audio.notchClip == null && clip != null)
                {
                    audio.notchClip = clip;
                }

                EnsureSourceAndSubscribe(audio, typeof(RotaryAudio), "notchSound", "PlaySound", audio.notchClip ?? clip, stepped);
            }
        }

        static void EnsureSourceAndSubscribe(Component audio, Type audioType, string sourceField, string playMethod, AudioClip? clip, SteppedJoint stepped)
        {
            if (audio == null || stepped == null)
            {
                return;
            }

            var field = AccessTools.Field(audioType, sourceField);
            var source = field?.GetValue(audio) as AudioSource;
            if (source == null && clip != null)
            {
                source = audio.gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.minDistance = 0.15f;
                source.maxDistance = 12f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.clip = clip;
                source.volume = 1f;
                field?.SetValue(audio, source);
            }
            else if (source != null && source.clip == null && clip != null)
            {
                source.clip = clip;
            }

            if (wiredAudio.Contains(audio.GetInstanceID()))
            {
                return;
            }

            if (AlreadySubscribed(stepped, audio))
            {
                wiredAudio.Add(audio.GetInstanceID());
                return;
            }

            var play = AccessTools.Method(audioType, playMethod);
            if (play == null)
            {
                return;
            }

            var handler = Delegate.CreateDelegate(typeof(Action<ValueChangedEventArgs>), audio, play);
            stepped.PositionChanged += (Action<ValueChangedEventArgs>)handler;
            wiredAudio.Add(audio.GetInstanceID());
        }

        static bool AlreadySubscribed(SteppedJoint stepped, Component audio)
        {
            var del = AccessTools.Field(typeof(SteppedJoint), "PositionChanged")?.GetValue(stepped) as Delegate;
            if (del == null)
            {
                return false;
            }

            foreach (var entry in del.GetInvocationList())
            {
                if (ReferenceEquals(entry.Target, audio))
                {
                    return true;
                }
            }

            return false;
        }

        static void Resolve(GameObject go, out float spring, out float damper, out AudioClip? clip)
        {
            spring = FallbackSpring;
            damper = FallbackDamper;
            clip = ExistingClip(go);

            if (TryDonor(out var donorSpring, out var donorDamper, out var donorClip))
            {
                if (donorSpring > 1f)
                {
                    spring = donorSpring;
                }

                if (donorDamper > 0.01f)
                {
                    damper = donorDamper;
                }

                if (clip == null)
                {
                    clip = donorClip;
                }
            }

            if (clip == null)
            {
                clip = go.GetComponentInChildren<LeverAudio>(true)?.hitClip
                       ?? go.GetComponentInChildren<Lever>(true)?.limitHit;
            }
        }

        static AudioClip? ExistingClip(GameObject go)
        {
            var lever = go.GetComponentInChildren<Lever>(true);
            if (lever?.notch != null)
            {
                return lever.notch;
            }

            var rotary = go.GetComponentInChildren<Rotary>(true);
            if (rotary?.notch != null)
            {
                return rotary.notch;
            }

            var audio = go.GetComponentInChildren<LeverAudio>(true);
            if (audio?.notchClip != null)
            {
                return audio.notchClip;
            }

            return go.GetComponentInChildren<RotaryAudio>(true)?.notchClip;
        }

        static bool TryDonor(out float spring, out float damper, out AudioClip? clip)
        {
            spring = 0f;
            damper = 0f;
            clip = null;

            try
            {
                var spawner = SingletonBehaviour<CarSpawner>.Instance;
                if (spawner?.AllLocos == null)
                {
                    return false;
                }

                foreach (var loco in spawner.AllLocos)
                {
                    if (loco == null || !loco.IsInteriorLoaded || loco.loadedInterior == null)
                    {
                        continue;
                    }

                    if (loco.carType != TrainCarType.LocoShunter &&
                        loco.carType != TrainCarType.LocoDiesel &&
                        loco.carType != TrainCarType.LocoDH4)
                    {
                        continue;
                    }

                    var mgr = loco.loadedInterior.GetComponentInChildren<InteriorControlsManager>(true);
                    if (mgr == null ||
                        !mgr.TryGetControl(InteriorControlsManager.ControlType.TrainBrake, out var reference) ||
                        reference.controlImplBase == null)
                    {
                        continue;
                    }

                    var donorGo = reference.controlImplBase.gameObject;
                    var lever = donorGo.GetComponentInChildren<Lever>(true);
                    if (lever != null)
                    {
                        spring = lever.jointSpring;
                        damper = lever.jointDamper;
                        clip = lever.notch ?? donorGo.GetComponentInChildren<LeverAudio>(true)?.notchClip;
                        return spring > 1f || clip != null;
                    }

                    var rotary = donorGo.GetComponentInChildren<Rotary>(true);
                    if (rotary != null)
                    {
                        spring = rotary.jointSpring;
                        damper = rotary.jointDamper;
                        clip = rotary.notch ?? donorGo.GetComponentInChildren<RotaryAudio>(true)?.notchClip;
                        return spring > 1f || clip != null;
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
