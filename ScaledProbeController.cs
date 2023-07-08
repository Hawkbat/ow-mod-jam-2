using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public class ScaledProbeController : ScaledCharacterController
    {
        SurveyorProbe probe;

        protected override void Awake()
        {
            base.Awake();
            probe = GetComponent<SurveyorProbe>();
        }

        public override float SizeMultiplier => 0.5f;
        protected override void UpdateScale(float newScale, float oldScale)
        {
            base.UpdateScale(newScale, oldScale);
            var probeScale = Vector3.one * (probe.IsLaunched() ? Scale : 1f);
            probe.transform.localScale = probeScale;
        }

        void Update()
        {
            if (!probe.IsLaunched())
            {
                if (PlayerState.IsInsideShip() && PlayerState.IsAttached())
                {
                    Scale = EscapePodFour.ScaledShip.Scale;
                }
                else
                {
                    Scale = EscapePodFour.ScaledPlayer.Scale;
                }
            }
        }
    }
}
