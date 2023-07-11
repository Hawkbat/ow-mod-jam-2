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

        public SurveyorProbe Probe => probe;

        protected override void Awake()
        {
            base.Awake();
            probe = GetComponent<SurveyorProbe>();
        }

        public override float SizeMultiplier => 0.5f;

        public override bool IsEmittingLight() => probe && probe.IsLaunched();

        protected override void UpdateScale(float newScale, float oldScale)
        {
            base.UpdateScale(newScale, oldScale);
            var probeScale = Vector3.one * (probe.IsLaunched() ? Scale : 1f);
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(probeScale.x / transform.lossyScale.x, probeScale.y / transform.lossyScale.y, probeScale.z / transform.lossyScale.z);
        }
    }
}
