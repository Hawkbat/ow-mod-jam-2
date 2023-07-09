using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace EscapePodFour
{
    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.OnCaughtObject))]
        public static bool AnglerfishController_OnCaughtObject(AnglerfishController __instance, OWRigidbody caughtBody)
        {
            var scaleCtrl = __instance.gameObject.GetComponent<ScaledAnglerfishController>();
            if (scaleCtrl == null) return true;
            var otherCtrl = caughtBody.GetComponent<ScaledCharacterController>();
            if (otherCtrl == null) return true;

            if (otherCtrl.Size > scaleCtrl.Size * 0.5f) return false;
            
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(FogWarpVolume), nameof(FogWarpVolume.WarpDetector))]
        public static void FogWarpVolume_WarpDetector(FogWarpVolume __instance, FogWarpDetector detector, FogWarpVolume linkedWarpVolume)
        {
            if (detector == null) EscapePodFour.LogError("detector NULL");
            if (linkedWarpVolume == null) EscapePodFour.LogError("linkedWarpVolume NULL");
            if (detector.GetOWRigidbody() == null) EscapePodFour.LogError("Detector OWRigidbody NULL");
            if (__instance._attachedBody == null) EscapePodFour.LogError("_attachedBody NULL");
            if (__instance._sector == null) EscapePodFour.LogError("_sector NULL");
            if (__instance._sector.GetTriggerVolume() == null) EscapePodFour.LogError("_sector.GetTriggerVolume() NULL");
        }
    }
}
