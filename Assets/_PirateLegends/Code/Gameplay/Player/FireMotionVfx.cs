using UnityEngine;

namespace PirateLegends.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class FireMotionVfx : MonoBehaviour
    {
        [SerializeField] private Material flameMaterial;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private Animator animator;
        private PlayerLocomotion motor;
        private FireAbility ability;
        private Transform playerRoot;
        private ParticleSystem leftFlame, rightFlame, leftEmbers, rightEmbers, leftHand, rightHand;
        private bool wasAirborne, leftContact, rightContact;
        private float lastLeftStep = -1f, lastRightStep = -1f;
        private bool initialized;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            motor = GetComponentInParent<PlayerLocomotion>();
            ability = GetComponent<FireAbility>();
            playerRoot = motor ? motor.transform : transform;
            if (!flameMaterial || !animator.isHuman) { enabled = false; return; }
            leftFlame = Create(HumanBodyBones.LeftFoot, "FireMotion_LeftBoot", false, false);
            rightFlame = Create(HumanBodyBones.RightFoot, "FireMotion_RightBoot", false, false);
            leftEmbers = Create(HumanBodyBones.LeftFoot, "FireMotion_LeftEmbers", true, false);
            rightEmbers = Create(HumanBodyBones.RightFoot, "FireMotion_RightEmbers", true, false);
            leftHand = Create(HumanBodyBones.LeftHand, "FireMotion_LeftHand", false, true);
            rightHand = Create(HumanBodyBones.RightHand, "FireMotion_RightHand", false, true);
            initialized = true;
        }

        private ParticleSystem Create(HumanBodyBones bone, string objectName, bool ember, bool hand)
        {
            var target = animator.GetBoneTransform(bone);
            var obj = new GameObject(objectName);
            obj.transform.position = target.position;
            obj.transform.SetParent(target, true);
            var ps = obj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(ember ? .22f : .18f, ember ? .45f : .4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(ember ? .4f : .08f, ember ? 1.1f : .3f);
            main.startSize = new ParticleSystem.MinMaxCurve(ember ? .012f : hand ? .065f : .11f,
                ember ? .028f : hand ? .13f : .22f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-.35f, .35f);
            main.startColor = new Color(1f, .4f, .055f, hand ? .6f : .85f);
            main.gravityModifier = ember ? -.07f : -.025f;
            main.maxParticles = ember ? 36 : hand ? 28 : 64;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = hand ? .035f : .065f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(.25f, ember ? .7f : .55f);
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(new Color(1f, .92f, .45f), 0f),
                new GradientColorKey(new Color(1f, .3f, .015f), .35f),
                new GradientColorKey(new Color(.6f, .025f, .002f), 1f)
            }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.9f, .12f),
                new GradientAlphaKey(.5f, .55f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, ember ? 1f : .35f), new Keyframe(.2f, 1f), new Keyframe(1f, 0f)));
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = flameMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = .12f;
            ps.Play();
            return ps;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool attack = ability && ability.IsActive;
            bool airborne = state.IsName("Jump") || (motor && motor.enabled && !motor.Grounded);
            bool moving = !attack && !airborne && animator.GetFloat(Speed) > .08f;
            float speed = Mathf.Clamp(animator.GetFloat(Speed), 0f, 3f);
            float bootRate = moving ? Mathf.Lerp(18f, 40f, speed / 3f) : airborne ? 9f : 0f;
            SetRate(leftFlame, bootRate); SetRate(rightFlame, bootRate);
            SetRate(leftEmbers, moving ? speed * 5f : airborne ? 5f : 0f);
            SetRate(rightEmbers, moving ? speed * 5f : airborne ? 5f : 0f);
            float handRate = attack ? 0f : airborne ? 15f : moving ? 6f + speed * 3f : 3f;
            SetRate(leftHand, handRate); SetRate(rightHand, handRate);
            if (airborne && !wasAirborne) Burst(9, 10);
            if (!airborne && wasAirborne) Burst(12, 16);
            wasAirborne = airborne;
            Step(leftFlame, leftEmbers, moving, ref leftContact, ref lastLeftStep);
            Step(rightFlame, rightEmbers, moving, ref rightContact, ref lastRightStep);
        }

        private void Step(ParticleSystem flame, ParticleSystem embers, bool moving, ref bool previous, ref float last)
        {
            bool contact = moving && flame.transform.position.y - playerRoot.position.y < .24f;
            if (contact && !previous && Time.time - last > .16f)
            {
                flame.Emit(5); embers.Emit(7); last = Time.time;
            }
            previous = contact;
        }

        private void Burst(int flames, int sparks)
        {
            leftFlame.Emit(flames); rightFlame.Emit(flames);
            leftEmbers.Emit(sparks); rightEmbers.Emit(sparks);
        }

        private static void SetRate(ParticleSystem ps, float rate)
        {
            var emission = ps.emission;
            emission.rateOverTime = rate;
        }

        private void OnEnable()
        {
            if (!initialized) return;
            leftFlame.Play(); rightFlame.Play(); leftEmbers.Play(); rightEmbers.Play();
            leftHand.Play(); rightHand.Play();
        }

        private void OnDisable()
        {
            if (!initialized) return;
            leftFlame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rightFlame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            leftEmbers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rightEmbers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            leftHand.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rightHand.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            wasAirborne = leftContact = rightContact = false;
        }
    }
}
