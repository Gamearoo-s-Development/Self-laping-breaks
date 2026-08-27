using UnityEngine;
using UnityModManagerNet;

namespace SelfLappingBrakes
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Header("Self-lapping train brake (like DE2)")]
        [Draw("DM3")]
        public bool Dm3 = true;

        [Draw("S060")]
        public bool S060 = true;

        [Draw("S282")]
        public bool S282 = true;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }
}
