using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace SelfLappingBrakes
{
    public static class Main
    {
        public static UnityModManager.ModEntry? ModEntry { get; private set; }
        public static Settings Settings { get; private set; } = new Settings();
        public static bool Enabled { get; private set; }
        public static Harmony? Harmony { get; private set; }

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnUnload = OnUnload;

            try
            {
                Harmony = new Harmony(modEntry.Info.Id);
                Harmony.PatchAll(Assembly.GetExecutingAssembly());
                Enabled = true;
                WorldApply.ApplyAll();
                Log("Loaded Self-Lapping Brakes for DM3, S060, and S282.");
                return true;
            }
            catch (Exception ex)
            {
                modEntry.Logger.LogException("Failed to load Self-Lapping Brakes:", ex);
                Harmony?.UnpatchAll(modEntry.Info.Id);
                return false;
            }
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            WorldApply.ApplyAll();
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
            WorldApply.ApplyAll();
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            Harmony?.UnpatchAll(modEntry.Info.Id);
            Enabled = false;
            return true;
        }

        public static void Log(string message) => ModEntry?.Logger.Log(message);
        public static void Warning(string message) => ModEntry?.Logger.Warning(message);
    }
}
