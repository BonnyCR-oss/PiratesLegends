using UnityEngine;
using PirateLegends.Player;
using PirateLegends.Ships;

namespace PirateLegends.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform playerRoot;
        [SerializeField, Min(.5f)] private float distance = 4f;
        [SerializeField, Min(.01f)] private float sensitivity = .12f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField] private float pitch = 15f;
        [SerializeField] private float yaw;
        [SerializeField, Min(.05f)] private float collisionRadius = .2f;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private float currentDistance, distanceVelocity;
        private float helmBlend;
        private PlayerLocomotion locomotion;
        private PirateShipHelm lastHelm;
        public void Configure(Transform follow, PlayerInputReader reader, Transform root)
        {
            target = follow; input = reader; playerRoot = root;
            locomotion = root.GetComponent<PlayerLocomotion>();
            yaw = root.eulerAngles.y; currentDistance = distance;
        }
        private void Awake()
        {
            currentDistance = distance;
            if (playerRoot) locomotion = playerRoot.GetComponent<PlayerLocomotion>();
        }
        private void LateUpdate()
        {
            if (!target || !input) return;
            PirateShipHelm ship = locomotion ? locomotion.RidingShip : null;
            bool steering = ship && ship.IsSteering;
            if (steering) lastHelm = ship;
            helmBlend = Mathf.MoveTowards(helmBlend, steering ? 1f : 0f, Time.deltaTime / .55f);
            Vector2 look = steering ? Vector2.zero : input.Look;
            yaw = Mathf.Repeat(yaw + look.x * sensitivity, 360f);
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 backward = rotation * Vector3.back;
            float allowedDistance = distance;
            int count = Physics.SphereCastNonAlloc(target.position, collisionRadius, backward, hits,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (playerRoot && hits[i].collider.transform.IsChildOf(playerRoot)) continue;
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(.05f, hits[i].distance - .05f));
            }
            currentDistance = allowedDistance < currentDistance ? allowedDistance :
                Mathf.SmoothDamp(currentDistance, allowedDistance, ref distanceVelocity, .12f);
            Vector3 position = target.position + backward * currentDistance;
            if (lastHelm && helmBlend > 0f)
            {
                Transform hull = lastHelm.transform;
                Vector3 focus = hull.position + hull.forward * 5f + Vector3.up * 4.5f;
                Vector3 overview = hull.position - hull.forward * 41f + Vector3.up * 18f;
                Vector3 offset = overview - focus;
                int obstacles = Physics.SphereCastNonAlloc(focus, .4f, offset.normalized, hits,
                    offset.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float clearance = offset.magnitude;
                for (int i = 0; i < obstacles; i++)
                {
                    Transform obstacle = hits[i].collider.transform;
                    if (obstacle.IsChildOf(hull) || (playerRoot && obstacle.IsChildOf(playerRoot))) continue;
                    clearance = Mathf.Min(clearance, Mathf.Max(1f, hits[i].distance - .4f));
                }
                overview = focus + offset.normalized * clearance;
                float blend = Mathf.SmoothStep(0f, 1f, helmBlend);
                position = Vector3.Lerp(position, overview, blend);
                rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(focus - overview), blend);
            }
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
