using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EscapePodFour
{
    public class ScaledAnglerfishController : ScaledCharacterController
    {
        const float INITIAL_ACCELERATION = 40f;
        const float INITIAL_ARRIVAL_DISTANCE = 100f;
        const float INITIAL_CHASE_SPEED = 75f;
        const float INITIAL_ESCAPE_DISTANCE = 400f;
        const float INITIAL_INVESTIGATE_SPEED = 20f;
        const float INITIAL_PURSUE_DISTANCE = 300f;
        static Vector3 INITIAL_MOUTH_OFFSET = new(0f, 2f, 60f);

        public static List<ScaledAnglerfishController> All = new();

        AnglerfishController angler;

        public override float SizeMultiplier => 50f;

        protected override void UpdateScale(float newScale, float oldScale)
        {
            base.UpdateScale(newScale, oldScale);
            angler._acceleration = INITIAL_ACCELERATION * Scale;
            angler._arrivalDistance = INITIAL_ARRIVAL_DISTANCE * Scale;
            angler._chaseSpeed = INITIAL_CHASE_SPEED * Scale;
            angler._escapeDistance = INITIAL_ESCAPE_DISTANCE * Scale;
            angler._investigateSpeed = INITIAL_INVESTIGATE_SPEED * Scale;
            angler._pursueDistance = INITIAL_PURSUE_DISTANCE * Scale;
            angler._mouthOffset = INITIAL_MOUTH_OFFSET * Scale;
        }

        void OnEnable()
        {
            All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        protected override void Awake()
        {
            base.Awake();
            angler = GetComponent<AnglerfishController>();
        }
    }
}
