using UnityEngine;

namespace PirateLegends.Player
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class PlayerLocomotion : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField, Min(.1f)] private float walkSpeed = 2.5f;
        [SerializeField, Min(.1f)] private float runSpeed = 5.5f;
        [SerializeField, Min(.1f)] private float acceleration = 18f;
        [SerializeField, Min(.1f)] private float rotationSpeed = 720f;
        [SerializeField, Min(.1f)] private float jumpHeight = 1.25f;
        [SerializeField] private float gravity = -24f;
        [SerializeField, Min(0f)] private float coyoteTime = .10f;
        [SerializeField, Min(0f)] private float jumpBuffer = .12f;
        private CharacterController body;
        private PlayerInputReader input;
        private Vector3 horizontalVelocity;
        private float verticalVelocity, lastGrounded = float.NegativeInfinity;
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Jump = Animator.StringToHash("Jump");
        public bool Grounded { get; private set; }
        public Vector3 Velocity => body.velocity;
        public Transform CameraTransform { set => cameraTransform = value; }

        private void Awake()
        {
            body = GetComponent<CharacterController>(); input = GetComponent<PlayerInputReader>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            if (animator) animator.applyRootMotion = false;
        }
        private bool ProbeGround()
        {
            float radius = body.radius * .85f;
            Vector3 foot = transform.TransformPoint(body.center) - Vector3.up * (body.height * .5f);
            int count = Physics.SphereCastNonAlloc(foot + Vector3.up * (radius + .15f), radius,
                Vector3.down, groundHits, .23f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) <= body.slopeLimit) return true;
            }
            return false;
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Grounded = verticalVelocity <= 0f && ProbeGround();
            if (Grounded) { lastGrounded = Time.time; verticalVelocity = -2f; }
            Vector2 move = input.Move;
            Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
            forward.y = 0f; forward.Normalize();
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity,
                direction * (input.Sprint ? runSpeed : walkSpeed), acceleration * dt);
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), rotationSpeed * dt);
            if (Time.time - lastGrounded <= coyoteTime && input.ConsumeJump(jumpBuffer))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastGrounded = float.NegativeInfinity; Grounded = false;
                if (animator) animator.SetTrigger(Jump);
            }
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -40f);
            body.stepOffset = Grounded ? .3f : 0f;
            var flags = body.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if (animator)
            {
                var actual = body.velocity; actual.y = 0;
                float speed = actual.magnitude;
                float value = speed <= walkSpeed ? speed / walkSpeed : Mathf.Lerp(1f, 3f, Mathf.InverseLerp(walkSpeed, runSpeed, speed));
                animator.SetFloat(Speed, value, .10f, dt);
            }
        }
    }
}
