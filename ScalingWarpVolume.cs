﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public class ScalingWarpVolume : MonoBehaviour
    {
        public float ScaleChange = 1f;

        FogWarpVolume warpVolume;

        void Awake()
        {
            warpVolume = GetComponent<FogWarpVolume>();
            warpVolume.OnWarpDetector += WarpVolume_OnWarpDetector;

            ScaleChange = EscapePodFour.GetScaleChange(warpVolume);
        }

        void OnDestroy()
        {
            warpVolume.OnWarpDetector -= WarpVolume_OnWarpDetector;
        }

        private void WarpVolume_OnWarpDetector(FogWarpDetector detector)
        {
            EscapePodFour.Log($"{(warpVolume.IsOuterWarpVolume() ? transform.root.name : name)} warped {detector.transform.root.name}");
            if (ScaleChange != 1f)
            {
                var scaleCtrl = detector.transform.root.GetComponent<ScaledCharacterController>();
                if (scaleCtrl != null)
                {
                    scaleCtrl.TransitWarpVolume(warpVolume);

                    EscapePodFour.Log($"{scaleCtrl.name} changing scale by {ScaleChange} (Old: {scaleCtrl.Scale}, New: {scaleCtrl.Scale * ScaleChange})");
                    scaleCtrl.Scale *= ScaleChange;

                    if (scaleCtrl is ScaledShipController && PlayerState.IsAttached() && PlayerState.IsInsideShip())
                    {
                        var player = EscapePodFour.ScaledPlayer;
                        EscapePodFour.Log($"{player.transform.name} changing scale by {ScaleChange} (Old: {player.Scale}, New: {player.Scale * ScaleChange})");
                        EscapePodFour.ScaledPlayer.Scale *= ScaleChange;
                    }

                    foreach (var item in scaleCtrl.gameObject.GetComponentsInChildren<ScaledItemController>(true))
                    {
                        EscapePodFour.Log($"{item.transform.name} changing scale by {ScaleChange} (Old: {item.Scale}, New: {item.Scale * ScaleChange})");
                        item.Scale *= ScaleChange;
                    }
                }
            }
        }
    }
}
