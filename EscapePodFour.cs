using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapePodFour
{
    public class EscapePodFour : ModBehaviour
    {
        public const float SHIP_SIZE_RATIO = 8f;
        public const float PLAYER_SIZE_RATIO = 1f;
        public const float PROBE_SIZE_RATIO = 0.5f;
        public const float HOPPER_SIZE_RATIO = 2f;
        public const float ANGLER_SIZE_RATIO = 50f;

        static EscapePodFour instance;
        static INewHorizons newHorizons;
        static Tweaks tweaks;

        public static ScaledPlayerController ScaledPlayer;
        public static ScaledShipController ScaledShip;
        public static ScaledProbeController ScaledProbe;

        static List<SphericalFogWarpVolume> fogWarpVolumes = new();
        static List<OuterFogWarpVolume> outerFogWarpVolumes = new();
        static List<InnerFogWarpVolume> innerFogWarpVolumes = new();

        public static IEnumerable<SphericalFogWarpExit> GetFogWarpExits()
        {
            foreach (var fogWarpVolume in fogWarpVolumes)
            {
                foreach (var exit in fogWarpVolume._exits)
                {
                    yield return exit;
                }
            }
        }

        public static float GetScaleChange(FogWarpVolume warpVolume)
        {
            var key = warpVolume.IsOuterWarpVolume() ? warpVolume.transform.root.name.Replace("_Body", "") : warpVolume.name;
            if (tweaks.warpScaleChanges.TryGetValue(key, out var scaleChange))
            {
                return scaleChange;
            }
            return 1f;
        }

        public static void Log(string msg)
            => instance.ModHelper.Console.WriteLine(msg, MessageType.Info);

        public static void LogWarning(string msg)
            => instance.ModHelper.Console.WriteLine(msg, MessageType.Warning);

        public static void LogError(string msg)
            => instance.ModHelper.Console.WriteLine(msg, MessageType.Error);

        public static void LogSucess(string msg)
            => instance.ModHelper.Console.WriteLine(msg, MessageType.Success);

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            LogSucess($"{nameof(EscapePodFour)} is loaded!");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            newHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            newHorizons.LoadConfigs(this);

            tweaks = ModHelper.Storage.Load<Tweaks>("tweaks.json");
            if (tweaks == null || !tweaks.initialized)
            {
                LogError("tweaks.json is missing or broken!");
            }

            newHorizons.GetBodyLoadedEvent().AddListener(OnBodyLoaded);

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                
                ModHelper.Events.Unity.FireOnNextUpdate(() =>
                {
                    ScaledPlayer = Locator.GetPlayerBody().gameObject.AddComponent<ScaledPlayerController>();
                    ScaledShip = Locator.GetShipBody().gameObject.AddComponent<ScaledShipController>();
                    ScaledProbe = Locator.GetProbe().gameObject.AddComponent<ScaledProbeController>();

                    outerFogWarpVolumes = new(FindObjectsOfType<OuterFogWarpVolume>());
                    innerFogWarpVolumes = new(FindObjectsOfType<InnerFogWarpVolume>());
                    fogWarpVolumes = new();
                    fogWarpVolumes.AddRange(outerFogWarpVolumes);
                    fogWarpVolumes.AddRange(innerFogWarpVolumes);
                    foreach (var angler in FindObjectsOfType<AnglerfishController>())
                    {
                        angler.gameObject.AddComponent<ScaledAnglerfishController>();
                    }
                    foreach (var warpVolume in fogWarpVolumes)
                    {
                        warpVolume.gameObject.AddComponent<ScalingWarpVolume>();
                    }
                });
            };
        }

        private void WarpVolume_OnWarpDetector(FogWarpDetector detector)
        {
            throw new System.NotImplementedException();
        }

        void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                var suit = Locator.GetPlayerSuit();
                if (suit.IsWearingSuit()) suit.RemoveSuit();
                else suit.SuitUp();
            }
        }

        void OnGUI()
        {
            foreach (var v in fogWarpVolumes)
            {
                GUI.Label(new Rect(WorldToGui(v.transform.position), new Vector2(500f, 20f)), v.name);
                foreach (var e in v._exits)
                {
                    GUI.Label(new Rect(WorldToGui(v.GetExitPosition(e)), new Vector2(500f, 20f)), e.name);
                }
            }
            foreach (var h in HopperController.All)
            {
                GUI.Label(new Rect(WorldToGui(h.transform.position), new Vector2(500f, 20f)), $"{h.name} x{h.Scale} ({h.State} {h.Target})");
            }
        }

        void OnBodyLoaded(string bodyName)
        {
            if (bodyName == "DB_D_WHITEHOLE")
            {
                var root = newHorizons.GetPlanet(bodyName).transform;
                var volumeObj = root.Find("Sector/Volumes/ZeroG_Fluid_Audio_Volume").gameObject;
                var gravityObj = new GameObject("GravityVolume");
                gravityObj.transform.parent = volumeObj.transform.parent;
                gravityObj.transform.localPosition = Vector3.zero;
                gravityObj.layer = LayerMask.NameToLayer("BasicEffectVolume");
                gravityObj.SetActive(false);
                var sphereShape = gravityObj.AddComponent<SphereShape>();
                sphereShape.radius = volumeObj.GetComponent<SphereShape>().radius;
                var gravityVolume = gravityObj.AddComponent<PolarForceVolume>();
                gravityVolume._priority = 2;
                gravityVolume._acceleration = -10f;
                gravityObj.SetActive(true);
            }
        }

        Vector2 WorldToGui(Vector3 wp)
        {
            var c = Locator.GetPlayerCamera();
            var sp = c.WorldToScreenPoint(wp);
            if (sp.z < 0) return new Vector2(Screen.width, Screen.height);
            var gp = new Vector2(sp.x, Screen.height - sp.y);
            return gp;
        }
    }
}