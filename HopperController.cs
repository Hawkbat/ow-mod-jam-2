using System.Collections.Generic;
using UnityEngine;

namespace EscapePodFour
{
    public class HopperController : ScaledCharacterController
    {
        public static List<HopperController> All = new();

        const float REST_MASS = 100f;
        const float FLIGHT_MASS = 2f;
        
        const float REST_DRAG = 1f;
        const float FLIGHT_DRAG = 0f;
        
        const float IDLE_DELAY = 1f;
        const float JUMP_DELAY = 1.5f;
        const float LANDING_DELAY = 2f;

        const float JUMP_NOISE_RADIUS = 300f;

        const float RAYCAST_OFFSET = 1f;
        const float FLIGHT_RAYCAST_LENGTH = 10f;
        const float LANDING_RAYCAST_LENGTH = 1f;

        const float JUMP_FORCE = 20f;
        const float MAX_JUMP_CONE = 0.75f;
        const float REST_VELOCITY_LIMIT = 0.1f;
        
        const float PLAYER_DETECTION_RADIUS = 100f;
        const float SHIP_DETECTION_RADIUS = 150f;
        const float PROBE_DETECTION_RADIUS = 200f;
        const float FOG_EXIT_DETECTION_RADIUS = 150f;
        const float ANGLER_DETECTION_RADIUS = 200f;

        public ActionState State;
        public Transform Target;

        OWRigidbody body;
        ImpactSensor impactSensor;
        GenericNoiseMaker noiseMaker;

        bool scared;
        float stateTime;

        RaycastHit[] hitBuffer = new RaycastHit[8];

        public override bool IsEmittingLight() => false;

        protected override void Awake()
        {
            body = gameObject.GetComponent<OWRigidbody>();
            impactSensor = gameObject.GetComponent<ImpactSensor>();
            noiseMaker = gameObject.GetComponent<GenericNoiseMaker>();

            base.Awake();

            ChangeState(ActionState.Idle);

            impactSensor.OnImpact += ImpactSensor_OnImpact;
        }

        void Start()
        {
            var origParent = body.GetOrigParent();
            var volumeParent = origParent.Find("Volumes");
            if (volumeParent != null)
            {
                var triggerVolumes = volumeParent.GetComponentsInChildren<OWTriggerVolume>();
                foreach (var triggerVolume in triggerVolumes)
                {
                    triggerVolume.AddObjectToVolume(gameObject);
                }
            }
        }

        void OnEnable()
        {
            All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        public override float SizeMultiplier => 2f;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            switch (State)
            {
                case ActionState.Idle:
                    {
                        body.SetMass(REST_MASS * Scale);
                        body.SetVelocity(Vector3.zero);
                        noiseMaker.enabled = false;
                        if (Time.time > stateTime + IDLE_DELAY)
                        {
                            if (Target)
                            {
                                var ctrl = Target.GetComponent<ScaledCharacterController>();
                                if (ctrl.CurrentWarpVolume != CurrentWarpVolume && ctrl.PreviousWarpVolume == CurrentWarpVolume)
                                {
                                    foreach (var (warpVolume, exit) in EscapePodFour.GetFogWarpExits())
                                    {
                                        if (warpVolume == ctrl.PreviousWarpVolume && exit == warpVolume.FindClosestWarpExit(body.GetPosition()))
                                        {
                                            if (AttemptAcquireTarget(exit.transform, 0f, FOG_EXIT_DETECTION_RADIUS * Scale)) return;
                                        }
                                    }
                                }
                            }
                            if (AttemptAcquireTarget(EscapePodFour.ScaledProbe, PROBE_DETECTION_RADIUS * Scale)) return;
                            if (AttemptAcquireTarget(EscapePodFour.ScaledShip, SHIP_DETECTION_RADIUS * Scale)) return;
                            if (AttemptAcquireTarget(EscapePodFour.ScaledPlayer, PLAYER_DETECTION_RADIUS * Scale)) return;
                            foreach (var angler in ScaledAnglerfishController.All)
                            {
                                if (AttemptAcquireTarget(angler, ANGLER_DETECTION_RADIUS * Scale)) return;
                            }
                            Target = null;
                        }
                    }
                    break;
                case ActionState.Jumping:
                    {
                        body.SetMass(REST_MASS * Scale);
                        body.SetVelocity(Vector3.zero);
                        noiseMaker.enabled = true;
                        noiseMaker.NoiseRadius = JUMP_NOISE_RADIUS * Scale;
                        if (!Target)
                        {
                            ChangeState(ActionState.Idle);
                            return;
                        }
                        if (Time.time > stateTime + JUMP_DELAY)
                        {
                            var up = body.GetLocalUpDirection();
                            var targetDir = (Target.transform.position - transform.position).normalized;
                            if (scared)
                            {
                                targetDir = -targetDir;
                            }
                            if (Vector3.Dot(up, targetDir) < 1f - MAX_JUMP_CONE)
                            {
                                var plane = new Plane(up, Vector3.zero);
                                var flatDir = plane.ClosestPointOnPlane(targetDir).normalized;
                                targetDir = Vector3.Slerp(up, flatDir, MAX_JUMP_CONE);
                            }

                            body.AddVelocityChange(targetDir * JUMP_FORCE * Scale);
                            ChangeState(ActionState.Flying);
                        }
                    }
                    break;
                case ActionState.Flying:
                    {
                        body.SetMass(FLIGHT_MASS * Scale);
                        //body._rigidbody.drag = FLIGHT_DRAG;
                        noiseMaker.enabled = false;
                        if (Time.time > stateTime + LANDING_DELAY)
                        {
                            ChangeState(ActionState.Landing);
                            return;
                        }
                        var travelDir = body.GetVelocity().normalized;
                        var hitCount = Physics.RaycastNonAlloc(transform.position, travelDir, hitBuffer, (RAYCAST_OFFSET + FLIGHT_RAYCAST_LENGTH) * Scale);
                        for (int i = 0; i < hitCount; i++)
                        {
                            var hit = hitBuffer[i];
                            if (hit.collider && !hit.collider.isTrigger && hit.collider.transform.root != transform.root && hit.collider.transform.root != Target)
                            {
                                ChangeState(ActionState.Landing);
                                return;
                            }
                        }
                        var deltaRot = ToRadians(Quaternion.FromToRotation(transform.forward, travelDir));
                        body.RotateWithPhysics(deltaRot * Time.deltaTime);
                    }
                    break;
                case ActionState.Landing:
                    {
                        body.SetMass(REST_MASS * Scale);
                        //body._rigidbody.drag = REST_DRAG;
                        noiseMaker.enabled = false;
                        var travelDir = body.GetVelocity().normalized;
                        var hitCount = Physics.RaycastNonAlloc(transform.position, travelDir, hitBuffer, (RAYCAST_OFFSET + LANDING_RAYCAST_LENGTH) * Scale);
                        for (int i = 0; i < hitCount; i++)
                        {
                            var hit = hitBuffer[i];
                            if (hit.collider && !hit.collider.isTrigger && hit.collider.transform.root != transform.root && hit.collider.transform.root != Target)
                            {
                                ChangeState(ActionState.Idle);
                                return;
                            }
                        }
                        var deltaRot = ToRadians(Quaternion.FromToRotation(transform.up, -travelDir));
                        body.RotateWithPhysics(deltaRot * Time.deltaTime);
                        if (body.GetVelocity().magnitude < REST_VELOCITY_LIMIT)
                        {
                            ChangeState(ActionState.Idle);
                            return;
                        }
                    }
                    break;
            }
        }

        void ChangeState(ActionState state)
        {
            State = state;
            stateTime = Time.time;
        }

        bool AttemptAcquireTarget(ScaledCharacterController c, float detectionRadius)
        {
            if (!c.IsEmittingLight()) return false;
            return AttemptAcquireTarget(c.transform, c.Size, detectionRadius);
        }

        bool AttemptAcquireTarget(Transform t, float size, float detectionRadius)
        {
            if (Vector3.Distance(t.position, transform.position) < detectionRadius)
            {
                scared = size >= Size;
                Target = t.transform;
                ChangeState(ActionState.Jumping);
                return true;
            }
            return false;
        }

        void ImpactSensor_OnImpact(ImpactData impact)
        {
            if (impact.otherBody == Locator.GetPlayerBody() || impact.otherBody == Locator.GetShipBody())
            {
                return;
            }
            if (State == ActionState.Flying)
            {
                ChangeState(ActionState.Landing);
            }
            else if (State == ActionState.Landing)
            {
                ChangeState(ActionState.Idle);
            }
        }

        Vector3 ToRadians(Quaternion q)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return q.ToEulerAngles();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public enum ActionState
        {
            Idle,
            Jumping,
            Flying,
            Landing,
        }
    }
}
