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
        float playerScale;

        public override float SizeMultiplier => 8f;

        public override bool IsEmittingLight() => cockpitController.AreExternalLightsOn();

        protected override void Awake()
        {
            base.Awake();
            cockpitController = transform.root.GetComponentInChildren<ShipCockpitController>();
            logController = transform.root.GetComponentInChildren<ShipLogController>();
        }

        void Update()
        {
            var currentPlayerScale = EscapePodFour.ScaledPlayer.Scale;
            if (playerScale != currentPlayerScale)
            {
                playerScale = currentPlayerScale;
                UpdateScale(Scale, Scale);
            }
        }

        protected override void UpdateScale(float newScale, float oldScale)
        {
            base.UpdateScale(newScale, oldScale);
            cockpitController._origAttachPointLocalPos = new(0f, 2.1849f - 1.8496f * playerScale, 4.2307f + 0.15f - 0.15f * playerScale);
            logController._attachPoint._attachOffset = new(0f, 1.8496f - 1.8496f * playerScale, 0.15f - 0.15f * playerScale);

        }
    }
}
