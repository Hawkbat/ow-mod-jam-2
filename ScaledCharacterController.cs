using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public abstract class ScaledCharacterController : MonoBehaviour
    {
        public float Scale = 1f;

        public float Size { get => Scale * SizeMultiplier; set => Scale = value / SizeMultiplier; }
        public FogWarpVolume CurrentWarpVolume => currentWarpVolume;
        public FogWarpVolume PreviousWarpVolume => previousWarpVolume;
        public FogWarpVolume LastTransitWarpVolume => lastTransit;

        protected FogWarpDetector warpDetector;
        
        float previousScale;

        FogWarpVolume currentWarpVolume;
        FogWarpVolume previousWarpVolume;
        FogWarpVolume lastTransit;

        public abstract float SizeMultiplier { get; }

        protected virtual void Awake()
        {
            Scale = transform.localScale.z;
            warpDetector = transform.root.GetComponentInChildren<FogWarpDetector>();
            if (warpDetector != null)
            {
                warpDetector.OnTrackFogWarpVolume += WarpDetector_OnTrackFogWarpVolume;
                warpDetector.OnUntrackFogWarpVolume += WarpDetector_OnUntrackFogWarpVolume;
            }
        }

        protected virtual void Start()
        {
            if (warpDetector != null)
            {
                currentWarpVolume = warpDetector.GetOuterFogWarpVolume();
            }
        }

        protected virtual void OnDestroy()
        {
            if (warpDetector != null)
            {
                warpDetector.OnTrackFogWarpVolume -= WarpDetector_OnTrackFogWarpVolume;
                warpDetector.OnUntrackFogWarpVolume -= WarpDetector_OnUntrackFogWarpVolume;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (previousScale != Scale)
            {
                UpdateScale(Scale, previousScale);
                previousScale = Scale;
            }
        }

        public virtual bool IsValidTarget() => IsEmittingLight();

        public abstract bool IsEmittingLight();

        protected virtual void UpdateScale(float newScale, float oldScale)
        {
            transform.localScale = Vector3.one * newScale;
        }

        public void TransitWarpVolume(FogWarpVolume volume)
        {
            lastTransit = volume;
        }

        void WarpDetector_OnTrackFogWarpVolume(FogWarpVolume volume)
        {
            if (volume.IsOuterWarpVolume())
            {
                currentWarpVolume = volume;
                if (previousWarpVolume != null)
                {
                    EscapePodFour.Log($"{name} entering {volume.transform.root.name} from {previousWarpVolume.transform.root.name}");
                } else
                {
                    EscapePodFour.Log($"{name} entering {volume.transform.root.name}");
                }
            }
        }

        void WarpDetector_OnUntrackFogWarpVolume(FogWarpVolume volume)
        {
            if (volume.IsOuterWarpVolume())
            {
                EscapePodFour.Log($"{name} leaving {volume.transform.root.name}");
                previousWarpVolume = volume;
            }
        }
    }
}
