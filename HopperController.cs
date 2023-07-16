using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace EscapePodFour
{
    public class HopperController : ScaledCharacterController
    {
        public static List<HopperController> All = new();

        const float REST_MASS = 100f;
        const float FLIGHT_MASS = 2f;
        
        const float IDLE_DELAY = 1f;
        const float JUMP_DELAY = 1.5f;
        const float LANDING_DELAY = 2f;

        const float JUMP_NOISE_RADIUS = 300f;

        const float RAYCAST_OFFSET = 1f;
        const float FLIGHT_RAYCAST_LENGTH = 10f;
        const float LANDING_RAYCAST_LENGTH = 1f;

        const float JUMP_FORCE = 20f;
        const float CHASE_JUMP_CONE = 1f;
        const float FLEE_JUMP_CONE = 0.5f;
        const float REST_VELOCITY_LIMIT = 0.1f;
        
        const float PLAYER_DETECTION_RADIUS = 100f;
        const float SHIP_DETECTION_RADIUS = 150f;
        const float PROBE_DETECTION_RADIUS = 200f;
        const float ANGLER_DETECTION_RADIUS = 200f;

        public ActionState State;
        public ScaledCharacterController Target;

        public bool IsScared => scared;

        OWRigidbody body;
        ImpactSensor impactSensor;
        OWAudioSource audioSource;
        GenericNoiseMaker noiseMaker;
        Animator animator;
        SectorDetector sectorDetector;

        bool scared;
        float stateTime;

        RaycastHit[] hitBuffer = new RaycastHit[8];

        public override bool IsEmittingLight() => false;

        public override float SizeMultiplier => 4f;

        protected override void Awake()
        {
            base.Awake();
            body = gameObject.GetComponent<OWRigidbody>();
            impactSensor = gameObject.GetComponent<ImpactSensor>();
            audioSource = gameObject.GetComponent<OWAudioSource>();
            noiseMaker = gameObject.GetComponentInChildren<GenericNoiseMaker>();
            animator = gameObject.GetComponentInChildren<Animator>();
            sectorDetector = gameObject.GetComponentInChildren<SectorDetector>();

            impactSensor.OnImpact += ImpactSensor_OnImpact;

            ChangeState(ActionState.Idle);
        }

        protected override void Start()
        {
            var sector = body.GetOrigParent().GetComponentInParent<Sector>();
            EnterSector(sector);

            base.Start();
        }

        public void EnterSector(Sector sector)
        {
            sectorDetector.AddSector(sector);

            var volumeParent = sector.transform.Find("Volumes");
            var triggerVolumes = volumeParent.GetComponentsInChildren<OWTriggerVolume>();
            foreach (var triggerVolume in triggerVolumes)
            {
                triggerVolume.AddObjectToVolume(gameObject);
            }

            var warpVolumes = sector.transform.GetComponentsInChildren<FogWarpVolume>();
            foreach (var warpVolume in warpVolumes)
            {
                warpDetector.TrackFogWarpVolume(warpVolume);
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

        public float GetDetectionRadius(ScaledCharacterController target)
        {
            if (target is ScaledPlayerController) return PLAYER_DETECTION_RADIUS * Scale;
            else if (target is ScaledShipController) return SHIP_DETECTION_RADIUS * Scale;
            else if (target is ScaledProbeController) return PROBE_DETECTION_RADIUS * Scale;
            else if (target is ScaledAnglerfishController) return ANGLER_DETECTION_RADIUS * Scale;
            else return 0f;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            audioSource.pitch = 0.5f + (0.5f / Scale);
            switch (State)
            {
                case ActionState.Idle:
                    {
                        animator.SetBool("isIdle", true);
                        body.SetMass(REST_MASS * Scale);
                        body.SetVelocity(Vector3.zero);
                        noiseMaker.enabled = false;
                        if (Time.time > stateTime + IDLE_DELAY)
                        {
                            if (Target && AttemptAcquireTarget(Target, true)) return;
                            if (AttemptAcquireTarget(EscapePodFour.ScaledProbe)) return;
                            if (AttemptAcquireTarget(EscapePodFour.ScaledShip)) return;
                            if (AttemptAcquireTarget(EscapePodFour.ScaledPlayer)) return;
                            foreach (var angler in ScaledAnglerfishController.All)
                            {
                                if (AttemptAcquireTarget(angler)) return;
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
                        if (!audioSource.isPlaying) audioSource.Play();
                        if (!Target || !Target.IsValidTarget())
                        {
                            ChangeState(ActionState.Idle);
                            return;
                        }
                        if (Time.time > stateTime + JUMP_DELAY)
                        {
                            audioSource.Stop();
                            animator.SetBool("isIdle", false);
                            var up = body.GetLocalUpDirection();

                            var targetTransform = Target.transform;

                            if (Target.CurrentWarpVolume != CurrentWarpVolume)
                            {
                                var transit = Target.LastTransitWarpVolume;
                                if (transit == null)
                                {
                                    EscapePodFour.Log($"Target {Target} is in volume {Target.CurrentWarpVolume} not {CurrentWarpVolume} and has never warped");
                                } else if (transit.IsOuterWarpVolume())
                                {
                                    var closestExit = EscapePodFour.GetFogWarpExits()
                                        .Where(p => p.Item1 == transit)
                                        .Select(p => p.Item2)
                                        .OrderBy(p => Vector3.Distance(p.transform.position, transform.position))
                                        .FirstOrDefault();
                                    if (closestExit != null)
                                    {
                                        EscapePodFour.Log($"Target {Target} exited dimension, pursuing through {closestExit.transform.name}");
                                        targetTransform = closestExit.transform;
                                    }

                                } else
                                {
                                    EscapePodFour.Log($"Target {Target} entered node, pursuing through {transit.transform.name}");
                                    targetTransform = transit.transform;
                                }
                            }

                            var targetDir = (targetTransform.position - transform.position).normalized;
                            var jumpCone = CHASE_JUMP_CONE;
                            if (scared)
                            {
                                targetDir = -targetDir;
                                jumpCone = FLEE_JUMP_CONE;
                            }
                            if (Vector3.Dot(up, targetDir) < 1f - jumpCone)
                            {
                                var plane = new Plane(up, Vector3.zero);
                                var flatDir = plane.ClosestPointOnPlane(targetDir).normalized;
                                targetDir = Vector3.Slerp(up, flatDir, jumpCone);
                            }
                            body.AddVelocityChange(targetDir * JUMP_FORCE * Scale);
                            ChangeState(ActionState.Flying);
                        }
                    }
                    break;
                case ActionState.Flying:
                    {
                        body.SetMass(FLIGHT_MASS * Scale);
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

        bool AttemptAcquireTarget(ScaledCharacterController c, bool bypassDetectionRadius = false)
        {
            if (!c.IsValidTarget()) return false;
            if (bypassDetectionRadius || Vector3.Distance(c.transform.position, transform.position) < GetDetectionRadius(c))
            {
                scared = c.Size >= Size;
                Target = c;
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
            var angler = impact.otherBody.GetComponentInParent<ScaledAnglerfishController>();
            if (angler != null)
            {
                if (Size > angler.Size)
                {
                    EscapePodFour.Log($"Hopper x{Scale} ate Angler x{angler.Scale}");
                    Destroy(angler.gameObject);
                }
                else
                {
                    EscapePodFour.Log($"Angler x{angler.Scale} ate Hopper x{Scale}");
                    Destroy(gameObject);
                }
            }
            body.SetVelocity(Vector3.zero);
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
