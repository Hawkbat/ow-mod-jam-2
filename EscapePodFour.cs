using System.Collections.Generic;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapePodFour
{
    public class EscapePodFour : ModBehaviour
    {
        public static float PlayerSize = 1f;
        public static float ShipSize = 1f;
        public static float ProbeSize = 1f;

        static INewHorizons newHorizons;

        static PlayerBody player;
        static GameObject playerModel;
        static CapsuleCollider playerCollider;
        static CapsuleCollider detectorCollider;
        static CapsuleShape detectorShape;
        static GameObject playerThruster;
        static GameObject playerMarshmallowStick;
        static PlayerCameraController cameraController;

        static ShipBody ship;

        static SurveyorProbe probe;

        static List<SphericalFogWarpVolume> fogWarpVolumes = new();
        static List<OuterFogWarpVolume> outerFogWarpVolumes = new();
        static List<InnerFogWarpVolume> innerFogWarpVolumes = new();
        static List<AnglerfishController> anglers = new();

        public static IEnumerable<AnglerfishController> GetActiveAnglers()
        {
            foreach (var angler in anglers)
            {
                if (angler.isActiveAndEnabled) yield return angler;
            }
        }

        void Start()
        {
            ModHelper.Console.WriteLine($"My mod {nameof(EscapePodFour)} is loaded!", MessageType.Success);

            newHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
            newHorizons.LoadConfigs(this);

            newHorizons.GetBodyLoadedEvent().AddListener(OnBodyLoaded);

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);

                ModHelper.Events.Unity.FireOnNextUpdate(() =>
                {
                    player = Locator.GetPlayerBody() as PlayerBody;
                    playerCollider = player.GetComponent<CapsuleCollider>();
                    detectorCollider = player.transform.Find("PlayerDetector").GetComponent<CapsuleCollider>();
                    detectorShape = player.GetComponentInChildren<CapsuleShape>();
                    playerModel = player.transform.Find("Traveller_HEA_Player_v2").gameObject;
                    playerThruster = player.transform.Find("PlayerVFX").gameObject;
                    playerMarshmallowStick = player.transform.Find("RoastingSystem").transform.Find("Stick_Root").gameObject;
                    cameraController = Locator.GetPlayerCameraController();

                    ship = Locator.GetShipBody() as ShipBody;

                    probe = Locator.GetProbe();

                    outerFogWarpVolumes = new(FindObjectsOfType<OuterFogWarpVolume>());
                    innerFogWarpVolumes = new(FindObjectsOfType<InnerFogWarpVolume>());
                    fogWarpVolumes = new();
                    fogWarpVolumes.AddRange(outerFogWarpVolumes);
                    fogWarpVolumes.AddRange(innerFogWarpVolumes);
                    anglers = new(FindObjectsOfType<AnglerfishController>());
                });
            };
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

        void FixedUpdate()
        {
            if (player != null)
            {
                var playerScale = Vector3.one * PlayerSize;
                
                // Shamelessly adapted from https://github.com/Owen013/Smol-Hatchling/blob/master/Owen013.TeenyHatchling/SmolHatchlingController.cs
                playerModel.transform.localScale = playerScale / 10;
                playerModel.transform.localPosition = new Vector3(0, -1.03f, -0.2f * playerScale.z);
                playerThruster.transform.localScale = playerScale;
                playerThruster.transform.localPosition = new Vector3(0, -1 + playerScale.y, 0);
                cameraController._origLocalPosition = new Vector3(0f, -1 + 1.8496f * playerScale.y, 0.15f * playerScale.z);
                cameraController.transform.localPosition = cameraController._origLocalPosition;
                playerMarshmallowStick.transform.localPosition = new Vector3(0.25f, -1.8496f + 1.8496f * playerScale.y, 0.08f - 0.15f + 0.15f * playerScale.z);

                var height = 2 * playerScale.y;
                var radius = Mathf.Min(playerScale.z / 2, height / 2);
                var center = new Vector3(0, playerScale.y - 1, 0);
                playerCollider.height = detectorCollider.height = detectorShape.height = height;
                playerCollider.radius = detectorCollider.radius = detectorShape.radius = radius;
                playerCollider.center = detectorCollider.center = detectorShape.center = player._centerOfMass = playerCollider.center = detectorCollider.center = player._activeRigidbody.centerOfMass = center;
            }
            if (ship != null)
            {
                var shipScale = Vector3.one * ShipSize;
                ship.transform.localScale = shipScale;
            }
            if (probe != null)
            {
                if (!probe.IsLaunched())
                {
                    if (PlayerState.IsInsideShip() && PlayerState.IsAttached())
                    {
                        ProbeSize = ShipSize;
                    } else
                    {
                        ProbeSize = PlayerSize;
                    }
                }
                var probeScale = Vector3.one * (probe.IsLaunched() ? ProbeSize : 1f);
                probe.transform.localScale = probeScale;
            }
        }

        void OnGUI()
        {
            foreach (var v in fogWarpVolumes)
            {
                GUI.Label(new Rect(WorldToGui(v.transform.position), new Vector2(150f, 20f)), v.name);
                foreach (var e in v._exits)
                {
                    GUI.Label(new Rect(WorldToGui(v.GetExitPosition(e)), new Vector2(150f, 20f)), e.name);
                }
            }
            foreach (var h in HopperController.All)
            {
                GUI.Label(new Rect(WorldToGui(h.transform.position), new Vector2(250f, 20f)), $"{h.name} x{h.Size} ({h.State} {h.Target})");
            }
        }

        void OnBodyLoaded(string bodyName)
        {
            if (bodyName == "DB_D_WHITEHOLE")
            {
                var root = newHorizons.GetPlanet(bodyName).transform;
                var volumeObj = root.Find("Sector/Volumes/ZeroG_Fluid_Audio_Volume").gameObject;
                var gravityObj = new GameObject("GravityVolume");
                gravityObj.layer = LayerMask.NameToLayer("BasicEffectVolume");
                gravityObj.transform.parent = volumeObj.transform.parent;
                gravityObj.transform.localPosition = Vector3.zero;
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