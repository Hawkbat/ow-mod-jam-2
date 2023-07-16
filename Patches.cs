using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epic.OnlineServices;
using HarmonyLib;

namespace EscapePodFour
{
    [HarmonyPatch]
    public static class Patches
    {
        static float debounceTime;

        static void LogDebounced(string msg)
        {
            if (UnityEngine.Time.time > debounceTime)
            {
                debounceTime = UnityEngine.Time.time + 5f;
                EscapePodFour.Log(msg);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.OnCaughtObject))]
        public static bool AnglerfishController_OnCaughtObject(AnglerfishController __instance, OWRigidbody caughtBody)
        {
            var scaleCtrl = __instance.gameObject.GetComponent<ScaledAnglerfishController>();
            if (scaleCtrl == null) return true;
            var otherCtrl = caughtBody.GetComponent<ScaledCharacterController>();
            if (otherCtrl == null) return true;

            var canEat = otherCtrl.Size < scaleCtrl.Size * 0.9f;
            LogDebounced($"[Angler Eat] Angler: ^{scaleCtrl.Size} Other: {otherCtrl} ^{otherCtrl.Size} Result: {canEat}");
            if (!canEat)
            {
                return false;
            }
            
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(FirstPersonManipulator), nameof(FirstPersonManipulator.GetFocusedOWItem))]
        public static void FirstPersonManipulator_GetFocusedOWItem(ref OWItem __result)
        {
            if (__result != null)
            {
                var item = __result;
                var itemCtrl = item.GetComponent<ScaledItemController>();
                if (itemCtrl == null) return;
                var itemScale = itemCtrl.Scale;
                var playerScale = EscapePodFour.ScaledPlayer.Scale;
                var relativeScale = itemScale / playerScale;
                if (relativeScale > 2f || relativeScale < 0.5f)
                {
                    __result = null;
                }
                LogDebounced($"[Focus Item] Player: x{playerScale} Item: {item} x{itemScale} Result: {__result}");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OWItemSocket), nameof(OWItemSocket.AcceptsItem))]
        public static void OWItemSocket_AcceptsItem(OWItemSocket __instance, ref bool __result, OWItem item)
        {
            if (__result == false) return;
            var itemCtrl = item.GetComponent<ScaledItemController>();
            if (itemCtrl == null) return;
            var socketScale = __instance.transform.lossyScale.z;
            var itemScale = itemCtrl.Scale;
            var relativeScale = itemScale / socketScale;
            if (relativeScale > 1.2f || relativeScale < 0.8333f)
            {
                __result = false;
            }
            LogDebounced($"[Accepts Item] Socket: {__instance} x{socketScale} Item: {item} x{itemScale} Result: {__result}");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HighSpeedImpactSensor), nameof(HighSpeedImpactSensor.HandlePlayerInsideShip))]
        public static bool HighSpeedImpactSensor_HandlePlayerInsideShip()
        {
            return false;
        }
    }
}
