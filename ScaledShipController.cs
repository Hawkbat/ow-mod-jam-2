using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapePodFour
{
    public class ScaledShipController : ScaledCharacterController
    {
        ShipCockpitController cockpitController;
        ShipLogController logController;

        public override float SizeMultiplier => 6f;

        public override bool IsEmittingLight() => cockpitController.AreExternalLightsOn();

        protected override void Awake()
        {
            base.Awake();
            cockpitController = transform.root.GetComponentInChildren<ShipCockpitController>();
            logController = transform.root.GetComponentInChildren<ShipLogController>();
        }

        void OnEnable()
        {
            GlobalMessenger.AddListener("DetachPlayerFromPoint", OnDetachPlayerFromPoint);
        }

        void OnDisable()
        {
            GlobalMessenger.RemoveListener("DetachPlayerFromPoint", OnDetachPlayerFromPoint);
        }

        protected override void UpdateScale(float newScale, float oldScale)
        {
            base.UpdateScale(newScale, oldScale);
            var playerScale = EscapePodFour.ScaledPlayer.Scale;
            //cockpitController._origAttachPointLocalPos = new(0f, 2.1849f - 1.8496f * playerScale, 4.2307f + 0.15f - 0.15f * playerScale);
            //logController._attachPoint._attachOffset = new(0f, 1.8496f - 1.8496f * playerScale, 0.15f - 0.15f * playerScale);
        }

        void OnDetachPlayerFromPoint()
        {
            if (Scale != 1f || EscapePodFour.ScaledPlayer.Scale != 1f)
            {
                if (PlayerState.IsInsideShip())
                {
                    PlayerSpawner playerSpawner = FindObjectOfType<PlayerSpawner>();
                    playerSpawner.DebugWarp(playerSpawner.GetSpawnPoint(SpawnLocation.Ship));
                }
            }
        }
    }
}
