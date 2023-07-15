using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NAudio.CoreAudioApi;
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

        public static IEnumerable<(SphericalFogWarpVolume, SphericalFogWarpExit)> GetFogWarpExits()
        {
            foreach (var fogWarpVolume in fogWarpVolumes)
            {
                foreach (var exit in fogWarpVolume._exits)
                {
                    yield return (fogWarpVolume, exit);
                }
            }
        }

        public static float GetScaleChange(FogWarpVolume warpVolume)
        {
            var key = warpVolume.IsOuterWarpVolume() ? warpVolume.transform.root.name.Replace("_Body", "") : warpVolume.name;
            if (tweaks.warpScaleChanges.TryGetValue(key, out var scaleChange))
            {
                return scaleChange;
            } else if (key.StartsWith("DB_D_") || key.StartsWith("DB_N_"))
            {
                LogWarning($"No scale defined for {key}");
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
                    foreach (var item in FindObjectsOfType<OWItem>())
                    {
                        item.gameObject.AddComponent<ScaledItemController>();
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
            if (ScaledProbe && ScaledProbe.Probe && !ScaledProbe.Probe.IsLaunched())
            {
                if (PlayerState.IsInsideShip() && PlayerState.IsAttached())
                {
                    ScaledProbe.Scale = ScaledShip.Scale;
                }
                else
                {
                    ScaledProbe.Scale = ScaledPlayer.Scale;
                }
            }

            if (Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                var player = Locator.GetPlayerTransform();
                var planetRoots = Locator.GetPlayerSectorDetector()
                    ._sectorList
                    .Select(s => s.transform.root)
                    .Distinct();
                foreach (var planetRoot in planetRoots)
                {
                    var relativePosition = planetRoot.InverseTransformPoint(player.position);
                    var relativeRotation = planetRoot.InverseTransformRotation(player.rotation).eulerAngles;
                    var relativeNormal = planetRoot.InverseTransformDirection(player.up);

                    Log($@"Player location on {planetRoot.name}
""position"": {Vector3ToJsonString(relativePosition)},
""rotation"": {Vector3ToJsonString(relativeRotation)},
""normal"": {Vector3ToJsonString(relativeNormal)},");
                }
            }
            if (Keyboard.current.numpad8Key.wasPressedThisFrame)
            {
                ScaledPlayer.Scale *= 2f;
            }
            if (Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                ScaledPlayer.Scale *= 0.5f;
            }
            if (Keyboard.current.numpad9Key.wasPressedThisFrame)
            {
                ScaledShip.Scale *= 2f;
            }
            if (Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                ScaledShip.Scale *= 0.5f;
            }
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                var suit = Locator.GetPlayerSuit();
                if (suit.IsWearingSuit()) suit.RemoveSuit();
                else suit.SuitUp();
            }
        }

        void OnGUI()
        {
            GUILayout.BeginVertical();
            if (ScaledPlayer) GUILayout.Label($"Player x{ScaledPlayer.Scale}");
            if (ScaledShip) GUILayout.Label($"Ship x{ScaledShip.Scale}");
            if (ScaledProbe) GUILayout.Label($"Probe x{ScaledProbe.Scale}");
            GUILayout.EndVertical();
            foreach (var v in fogWarpVolumes)
            {
                DrawWorldLabel(v, v.name);
                foreach (var e in v._exits)
                {
                    DrawWorldLabel(v.GetExitPosition(e), e.name);
                }
            }
            foreach (var h in HopperController.All)
            {
                DrawWorldLabel(h, $"{h.name} x{h.Scale} ({h.State} {(h.Target ? h.IsScared ? "fleeing" : "chasing" : "")} {h.Target})");
            }
            foreach (var a in ScaledAnglerfishController.All)
            {
                DrawWorldLabel(a, $"{a.name} x{a.Scale} ({a.Angler.GetAnglerState()})");
            }
            foreach (var i in ScaledItemController.All)
            {
                DrawWorldLabel(i, $"{i.name} x{i.Scale}");
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
                gravityVolume._acceleration = -4f;
                gravityObj.SetActive(true);
            }
        }

        void DrawWorldLabel(Component component, string text)
        {
            DrawWorldLabel(component.transform.position, text);
        }

        void DrawWorldLabel(Vector3 worldPos, string text)
        {
            var c = Locator.GetPlayerCamera();
            var d = Vector3.Distance(c.transform.position, worldPos);
            if (d > 1000f) return;
            GUI.Label(new Rect(WorldToGui(worldPos), new Vector2(500f, 20f)), text);
        }

        Vector2 WorldToGui(Vector3 wp)
        {
            var c = Locator.GetPlayerCamera();
            var sp = c.WorldToScreenPoint(wp);
            if (sp.z < 0) return new Vector2(Screen.width, Screen.height);
            var gp = new Vector2(sp.x, Screen.height - sp.y);
            return gp;
        }
        string Vector3ToJsonString(Vector3 v)
            => $"{{\"x\": {v.x}, \"y\": {v.y}, \"z\": {v.z}}}";
    }
}