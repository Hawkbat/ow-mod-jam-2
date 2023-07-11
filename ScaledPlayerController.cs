using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public class ScaledPlayerController : ScaledCharacterController
    {
        PlayerBody player;
        GameObject playerModel;
        CapsuleCollider playerCollider;
        CapsuleCollider detectorCollider;
        CapsuleShape detectorShape;
        GameObject playerThruster;
        GameObject playerMarshmallowStick;
        PlayerCameraController cameraController;

        public override float SizeMultiplier => 1f;

        public override bool IsEmittingLight() => PlayerState.IsFlashlightOn() && !PlayerState.IsInsideShip();

        protected override void UpdateScale(float newScale, float oldScale)
        {
            // Shamelessly adapted from https://github.com/Owen013/Smol-Hatchling/blob/master/Owen013.TeenyHatchling/SmolHatchlingController.cs
            playerModel.transform.localScale = Vector3.one * newScale / 10f;
            playerModel.transform.localPosition = new Vector3(0, -1.03f, -0.2f * newScale);
            playerThruster.transform.localScale = Vector3.one * newScale;
            playerThruster.transform.localPosition = new Vector3(0, -1 + newScale, 0);
            cameraController._origLocalPosition = new Vector3(0f, -1 + 1.8496f * newScale, 0.15f * newScale);
            cameraController.transform.localPosition = cameraController._origLocalPosition;
            playerMarshmallowStick.transform.localPosition = new Vector3(0.25f, -1.8496f + 1.8496f * newScale, 0.08f - 0.15f + 0.15f * newScale);

            var height = 2 * newScale;
            var radius = Mathf.Min(newScale / 2, height / 2);
            var center = new Vector3(0, newScale - 1, 0);
            playerCollider.height = detectorCollider.height = detectorShape.height = height;
            playerCollider.radius = detectorCollider.radius = detectorShape.radius = radius;
            playerCollider.center = detectorCollider.center = detectorShape.center = player._centerOfMass = playerCollider.center = detectorCollider.center = player._activeRigidbody.centerOfMass = center;
        }

        protected override void Awake()
        {
            base.Awake();
            player = GetComponent<PlayerBody>();
            playerCollider = GetComponent<CapsuleCollider>();
            detectorCollider = transform.Find("PlayerDetector").GetComponent<CapsuleCollider>();
            detectorShape = GetComponentInChildren<CapsuleShape>();
            playerModel = transform.Find("Traveller_HEA_Player_v2").gameObject;
            playerThruster = transform.Find("PlayerVFX").gameObject;
            playerMarshmallowStick = transform.Find("RoastingSystem").transform.Find("Stick_Root").gameObject;
            cameraController = Locator.GetPlayerCameraController();
        }
    }
}
