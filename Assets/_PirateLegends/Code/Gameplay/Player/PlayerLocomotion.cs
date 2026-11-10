using UnityEngine;
using PirateLegends.Ships;
using PirateLegends.World;

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
        private FireAbility fireAbility;
        private OceanWeather water;
        private float lastDeckContact = float.NegativeInfinity;
        private Vector3 horizontalVelocity;
        private float verticalVelocity, lastGrounded = float.NegativeInfinity;
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Attack = Animator.StringToHash("Attack");
        public bool IsSwimming { get; private set; }
        public OceanWeather Water { set => water = value; }
        public bool Grounded { get; private set; }
        public PirateShipHelm RidingShip { get; private set; }
        public bool SteeringLocked { get; set; }
        private bool jumpingFromShip;
        public bool MovementLocked { get; set; }
        public Vector3 Velocity => body.velocity;
        public Transform CameraTransform { set => cameraTransform = value; }

        private void Awake()
        {
            body = GetComponent<CharacterController>(); input = GetComponent<PlayerInputReader>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            fireAbility = GetComponentInChildren<FireAbility>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            if (animator) animator.applyRootMotion = fireAbility != null;
        }
        private Collider ProbeGround(Vector3 carry)
        {
            float radius = body.radius * .85f;
            Vector3 foot = carry + transform.TransformPoint(body.center) - Vector3.up * (body.height * .5f);
            int count = Physics.SphereCastNonAlloc(foot + Vector3.up * (radius + .15f), radius,
                Vector3.down, groundHits, .23f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Collider closest = null;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance >= distance || Vector3.Angle(hit.normal, Vector3.up) > body.slopeLimit) continue;
                closest = hit.collider;
                distance = hit.distance;
            }
            return closest;
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var ship = RidingShip;
            Vector3 carry = ship ? ship.CarryDelta(transform.position) : Vector3.zero;
            Collider ground = (verticalVelocity <= 0f || IsSwimming) ? ProbeGround(carry) : null;
            Grounded = ground != null;
            if (Grounded || !jumpingFromShip)
                RidingShip = ground ? ground.GetComponentInParent<PirateShipHelm>() : null;
            if (Grounded) jumpingFromShip = false;
            if (ship && RidingShip == ship)
            {
                transform.rotation = ship.RotationDelta * transform.rotation;
                horizontalVelocity = ship.RotationDelta * horizontalVelocity;
            }
            else carry = Vector3.zero;
            if (Grounded && RidingShip) lastDeckContact = Time.time;
            UpdateSwimming(dt);
            if (Grounded) { lastGrounded = Time.time; verticalVelocity = -2f; }
            bool usingAbility = SteeringLocked || MovementLocked || (fireAbility && fireAbility.IsActive);
            Vector2 move = usingAbility ? Vector2.zero : input.Move;
            if (usingAbility) horizontalVelocity = Vector3.zero;
            Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
            forward.y = 0f; forward.Normalize();
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);
            float targetSpeed = IsSwimming ? walkSpeed * .8f : input.Sprint ? runSpeed : walkSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, direction * targetSpeed, acceleration * dt);
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), rotationSpeed * dt);
            if (!usingAbility && Time.time - lastGrounded <= coyoteTime && input.ConsumeJump(jumpBuffer))
            {
                jumpingFromShip = RidingShip != null;
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastGrounded = float.NegativeInfinity; Grounded = false;
                if (animator) animator.SetTrigger(Jump);
            }
            if (!IsSwimming) verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -40f);
            body.stepOffset = Grounded ? .3f : 0f;
            var flags = body.Move(carry + (horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if (animator)
            {
                if (!usingAbility && Grounded && input.AttackPressed && !animator.IsInTransition(0)
                    && !animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
                {
                    horizontalVelocity = Vector3.zero;
                    animator.SetTrigger(Attack);
                }
                var actual = body.velocity - carry / dt; actual.y = 0;
                float speed = actual.magnitude;
                float value = speed <= walkSpeed ? speed / walkSpeed : Mathf.Lerp(1f, 3f, Mathf.InverseLerp(walkSpeed, runSpeed, speed));
                animator.SetFloat(Speed, value, .10f, dt);
            }
        }

        private void UpdateSwimming(float dt)
        {
            bool wasSwimming = IsSwimming;
            IsSwimming = false;
            if (!water || Time.time - lastDeckContact <= .45f
                || !water.TryGetSurface(transform.position, out float surface)) return;

            float footHeight = transform.TransformPoint(body.center).y - body.height * .5f;
            float floatingHeight = surface - body.height * .6f;
            // Stay afloat through wave troughs and stand up when the shallows reach the capsule.
            if (footHeight > floatingHeight + (wasSwimming ? .12f : 0f) && (!wasSwimming || Grounded)) return;

            IsSwimming = true;
            Grounded = false;
            RidingShip = null;
            jumpingFromShip = false;
            lastGrounded = float.NegativeInfinity;
            input.ConsumeJump(float.PositiveInfinity);
            if (!wasSwimming) verticalVelocity = Mathf.Max(verticalVelocity, -2f);
            float floatSpeed = Mathf.Clamp((floatingHeight - footHeight) * 5f, -3f, 4f);
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, floatSpeed, 28f * dt);
        }

        public void ApplyAbilityMotion(Vector3 delta)
        {
            // Keep gravity and ground contact under the locomotion controller.
            if (SteeringLocked) return;
            delta.y = 0f;
            body.Move(delta);
        }


        public void TransferTo(Vector3 position)
        {
            body.enabled = false;
            transform.position = position;
            body.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            lastGrounded = lastDeckContact = float.NegativeInfinity;
            IsSwimming = false;
            Grounded = false;
            RidingShip = null;
            jumpingFromShip = false;
            input.ConsumeJump(float.PositiveInfinity);
        }
}
}
