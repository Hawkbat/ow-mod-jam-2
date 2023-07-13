using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public abstract class ScaledCharacterController : ScaledObjectController
    {
        public FogWarpVolume CurrentWarpVolume => currentWarpVolume;
        public FogWarpVolume PreviousWarpVolume => previousWarpVolume;
        public FogWarpVolume LastTransitWarpVolume => lastTransit;

        protected FogWarpDetector warpDetector;

        FogWarpVolume currentWarpVolume;
        FogWarpVolume previousWarpVolume;
        FogWarpVolume lastTransit;

        protected override void Awake()
        {
            base.Awake();
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
            } else
            {
                currentWarpVolume = transform
                    .GetAttachedOWRigidbody()
                    .GetOrigParentBody()
                    .GetComponentInChildren<OuterFogWarpVolume>();
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

        public virtual bool IsValidTarget() => IsEmittingLight();

        public abstract bool IsEmittingLight();

        public void TransitWarpVolume(FogWarpVolume volume)
        {
            lastTransit = volume;
        }

        protected virtual void OnChangeOuterWarpVolume(FogWarpVolume newVolume, FogWarpVolume oldVolume)
        {

        }

        void WarpDetector_OnTrackFogWarpVolume(FogWarpVolume volume)
        {
            if (volume.IsOuterWarpVolume())
            {
                currentWarpVolume = volume;
                OnChangeOuterWarpVolume(currentWarpVolume, previousWarpVolume);
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
