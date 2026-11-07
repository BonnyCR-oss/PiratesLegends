using PirateLegends.Player;
using UnityEngine;

namespace PirateLegends.UI
{
    // Only used by the test selector for characters that currently have a walk clip.
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class TestWalkMotor : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 1.8f;
        private Transform view;
        private CharacterController body;
        private PlayerInputReader input;
        private Animator animator;
        private float verticalSpeed;
        public void Configure(Transform cameraTransform) => view = cameraTransform;
        private void Awake()
        {
            body = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            animator = GetComponentInChildren<Animator>();
        }
        private void OnEnable() { verticalSpeed = 0; }
        private void Update()
        {
            Vector3 forward = view ? view.forward : Vector3.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 direction = Vector3.ClampMagnitude(forward * input.Move.y + Vector3.Cross(Vector3.up, forward) * input.Move.x, 1);
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 540 * Time.deltaTime);
            if (body.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(verticalSpeed - 24 * Time.deltaTime, -40);
            body.Move((direction * walkSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
            animator.speed = direction.magnitude;
        }
        private void OnDisable() { if (animator) animator.speed = 1; }
    }
}
