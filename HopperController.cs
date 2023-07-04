
using NAudio.CoreAudioApi;
using UnityEngine;

namespace EscapePodFour
{
    public class HopperController : MonoBehaviour
    {
        const float REST_MASS = 10f;
        const float FLIGHT_MASS = 2f;
        const float RAYCAST_OFFSET = 1f;
        const float RAYCAST_LENGTH = 10f;
        const float JUMP_FORCE = 20f;
        const float LANDING_DELAY = 1f;
        const float MAX_JUMP_CONE = 0.75f;
        const float JUMP_DELAY = 0f;
        const float REST_VELOCITY_LIMIT = 0.1f;
        const float DETECTION_RADIUS = 200f;

        public float Size = 1f;
        public ActionState State;

        OWRigidbody body;

        Transform target;
        bool scared;
        float stateTime;

        RaycastHit[] hitBuffer = new RaycastHit[8];

        void Awake()
        {
            body = gameObject.GetComponent<OWRigidbody>();
            ChangeState(ActionState.Idle);
        }

        void FixedUpdate()
        {
            switch (State)
            {
                case ActionState.Idle:
                    {
                        body.SetMass(REST_MASS);
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
                    break;
                case ActionState.Jumping:
                    {
                        body.SetMass(REST_MASS);
                        if (!target)
                        {
                            ChangeState(ActionState.Idle);
                            break;
                        }
                        if (Time.time > stateTime + JUMP_DELAY)
                        {
                            var up = body.GetLocalUpDirection();
                            var targetDir = (target.transform.position - transform.position).normalized;
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
                        if (Time.time > stateTime + LANDING_DELAY)
                        {
                            ChangeState(ActionState.Landing);
                            break;
                        }
                        var travelDir = body.GetVelocity().normalized;
                        var hitCount = Physics.RaycastNonAlloc(transform.position, travelDir, hitBuffer, (RAYCAST_OFFSET + RAYCAST_LENGTH) * Size);
                        for (int i = 0; i < hitCount; i++)
                        {
                            var hit = hitBuffer[i];
                            if (hit.collider && !hit.collider.isTrigger && hit.collider.transform.root != transform.root && hit.collider.transform.root != target)
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
                        var travelDir = body.GetVelocity().normalized;
                        var deltaRot = ToRadians(Quaternion.FromToRotation(transform.up, -travelDir));
                        body.RotateWithPhysics(deltaRot * Time.deltaTime);
                        if (body.GetVelocity().magnitude < REST_VELOCITY_LIMIT)
                        {
                            ChangeState(ActionState.Idle);
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
                target = t.transform;
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
