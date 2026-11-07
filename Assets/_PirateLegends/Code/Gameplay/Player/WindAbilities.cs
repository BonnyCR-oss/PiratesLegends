using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateLegends.Player
{
    [DefaultExecutionOrder(-110)]
    [RequireComponent(typeof(PlayerLocomotion), typeof(PlayerInputReader))]
    public sealed class WindAbilities : MonoBehaviour
    {
        [SerializeField] private float ability1Duration;
        [SerializeField] private float ability2Duration;
        private Animator animator;
        private PlayerLocomotion motor;
        private PlayerInputReader input;
        private float busyUntil;
        public bool IsDead { get; private set; }
        public bool IsBusy => IsDead || Time.time < busyUntil;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            motor = GetComponent<PlayerLocomotion>();
            input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (!input.enabled || !motor.enabled) return;
            motor.MovementLocked = IsBusy;
            var keys = Keyboard.current;
            if (keys == null || Cursor.lockState != CursorLockMode.Locked) return;
            if (keys.digit0Key.wasPressedThisFrame || keys.numpad0Key.wasPressedThisFrame) Revive();
            else if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) Die();
            else if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) TryUseAbility(1);
            else if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) TryUseAbility(2);
        }

        public bool TryUseAbility(int number)
        {
            if (!input.enabled || !motor.enabled || IsBusy || !motor.Grounded || (number != 1 && number != 2)) return false;
            busyUntil = Time.time + (number == 1 ? ability1Duration : ability2Duration);
            motor.MovementLocked = true;
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Jump");
            animator.CrossFadeInFixedTime("Ability" + number, .08f);
            return true;
        }

        public void Die()
        {
            if (!input.enabled || !motor.enabled || IsDead) return;
            IsDead = true;
            motor.MovementLocked = true;
            animator.ResetTrigger("Jump");
            animator.ResetTrigger("Attack");
            animator.CrossFadeInFixedTime("Death", .08f);
        }

        public void Revive()
        {
            if (!input.enabled || !motor.enabled) return;
            IsDead = false;
            busyUntil = 0;
            motor.MovementLocked = false;
            animator.CrossFadeInFixedTime("Locomotion", .1f);
        }

        private void OnDisable() { if (motor) motor.MovementLocked = false; }
    }
}
