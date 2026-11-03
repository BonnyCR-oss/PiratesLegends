using UnityEngine;
using PirateLegends.Player;

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
        public void Configure(Transform follow, PlayerInputReader reader, Transform root)
        {
            target = follow; input = reader; playerRoot = root;
            yaw = root.eulerAngles.y; currentDistance = distance;
        }
        private void Awake() { currentDistance = distance; }
        private void LateUpdate()
        {
            if (!target || !input) return;
            Vector2 look = input.Look;
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
            transform.SetPositionAndRotation(target.position + backward * currentDistance, rotation);
        }
    }
}
