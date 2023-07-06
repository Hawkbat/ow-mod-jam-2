using System.Collections.Generic;
using UnityEngine;

namespace EscapePodFour
{
    public class HopperController : MonoBehaviour
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
        
        const float DETECTION_RADIUS = 200f;

        public float Size = 1f;
        public ActionState State;
        public Transform Target;

        OWRigidbody body;
        GenericNoiseMaker noiseMaker;

        bool scared;
        float stateTime;

        RaycastHit[] hitBuffer = new RaycastHit[8];

        void Awake()
        {
            body = gameObject.GetComponent<OWRigidbody>();
            noiseMaker = gameObject.GetComponent<GenericNoiseMaker>();
            ChangeState(ActionState.Idle);
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

        void FixedUpdate()
        {
            transform.localScale = Vector3.one * Size;
            switch (State)
            {
                case ActionState.Idle:
                    {
                        body.SetMass(REST_MASS);
                        noiseMaker.enabled = false;
                        if (Time.time > stateTime + IDLE_DELAY)
                        {
                            if (AttemptAcquireTarget(Locator.GetProbe().transform, EscapePodFour.ProbeSize))
                                break;
                            if (AttemptAcquireTarget(Locator.GetShipTransform(), EscapePodFour.ShipSize))
                                break;
                            if (AttemptAcquireTarget(Locator.GetPlayerTransform(), EscapePodFour.PlayerSize))
                                break;
                            foreach (var angler in EscapePodFour.GetActiveAnglers())
                            {
                                if (AttemptAcquireTarget(angler.transform, angler.transform.localScale.z))
                                    break;
                            }
                        }
                    }
                    break;
                case ActionState.Jumping:
                    {
                        body.SetMass(REST_MASS);
                        noiseMaker.enabled = true;
                        noiseMaker.NoiseRadius = JUMP_NOISE_RADIUS;
                        if (!Target)
                        {
                            ChangeState(ActionState.Idle);
                            break;
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

                            body.AddVelocityChange(targetDir * JUMP_FORCE * Size);
                            ChangeState(ActionState.Flying);
                        }
                    }
                    break;
                case ActionState.Flying:
                    {
                        body.SetMass(FLIGHT_MASS);
                        body._rigidbody.drag = FLIGHT_DRAG;
                        noiseMaker.enabled = false;
                        if (Time.time > stateTime + LANDING_DELAY)
                        {
                            ChangeState(ActionState.Landing);
                            break;
                        }
                        var travelDir = body.GetVelocity().normalized;
                        var hitCount = Physics.RaycastNonAlloc(transform.position, travelDir, hitBuffer, (RAYCAST_OFFSET + FLIGHT_RAYCAST_LENGTH) * Size);
                        for (int i = 0; i < hitCount; i++)
                        {
                            var hit = hitBuffer[i];
                            if (hit.collider && !hit.collider.isTrigger && hit.collider.transform.root != transform.root && hit.collider.transform.root != Target)
                            {
                                ChangeState(ActionState.Landing);
                                break;
                            }
                        }
                        var deltaRot = ToRadians(Quaternion.FromToRotation(transform.forward, travelDir));
                        body.RotateWithPhysics(deltaRot * Time.deltaTime);
                    }
                    break;
                case ActionState.Landing:
                    {
                        body.SetMass(REST_MASS);
                        body._rigidbody.drag = REST_DRAG;
                        noiseMaker.enabled = false;
                        var travelDir = body.GetVelocity().normalized;
                        var hitCount = Physics.RaycastNonAlloc(transform.position, travelDir, hitBuffer, (RAYCAST_OFFSET + LANDING_RAYCAST_LENGTH) * Size);
                        for (int i = 0; i < hitCount; i++)
                        {
                            var hit = hitBuffer[i];
                            if (hit.collider && !hit.collider.isTrigger && hit.collider.transform.root != transform.root && hit.collider.transform.root != Target)
                            {
                                ChangeState(ActionState.Idle);
                                Target = null;
                                break;
                            }
                        }
                        var deltaRot = ToRadians(Quaternion.FromToRotation(transform.up, -travelDir));
                        body.RotateWithPhysics(deltaRot * Time.deltaTime);
                        if (body.GetVelocity().magnitude < REST_VELOCITY_LIMIT)
                        {
                            ChangeState(ActionState.Idle);
                            Target = null;
                            break;
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

        bool AttemptAcquireTarget(Transform t, float size)
        {
            if (Vector3.Distance(t.position, transform.position) < DETECTION_RADIUS)
            {
                scared = size >= Size;
                Target = t.transform;
                ChangeState(ActionState.Jumping);
                return true;
            }
            return false;
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
