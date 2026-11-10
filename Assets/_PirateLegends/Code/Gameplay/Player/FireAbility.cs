using UnityEngine;

namespace PirateLegends.Player
{
    [RequireComponent(typeof(Animator))]
    public sealed class FireAbility : MonoBehaviour
    {
        [SerializeField] private Material flameMaterial;
        private Animator animator;
        private PlayerLocomotion locomotion;
        private ParticleSystem leftFlames, rightFlames, leftSparks, rightSparks;
        private Light leftLight, rightLight;
        private bool emitting;

        public bool IsActive => animator && (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")
            || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsTag("Attack")));

        private void Awake()
        {
            animator = GetComponent<Animator>();
            locomotion = GetComponentInParent<PlayerLocomotion>();
            animator.applyRootMotion = true;
            CreateHand(HumanBodyBones.LeftHand, out leftFlames, out leftSparks, out leftLight);
            CreateHand(HumanBodyBones.RightHand, out rightFlames, out rightSparks, out rightLight);
        }

        private void CreateHand(HumanBodyBones bone, out ParticleSystem flames,
            out ParticleSystem sparks, out Light glow)
        {
            Transform hand = animator.GetBoneTransform(bone);
            flames = CreateEmitter(hand, "Fire_Flames", false);
            sparks = CreateEmitter(hand, "Fire_Embers", true);
            var obj = new GameObject("Fire_Glow");
            obj.transform.SetParent(hand, false);
            glow = obj.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, .22f, .025f);
            glow.range = 2.6f;
            glow.shadows = LightShadows.None;
            glow.intensity = 0f;
        }

        private ParticleSystem CreateEmitter(Transform hand, string emitterName, bool sparks)
        {
            var obj = new GameObject(emitterName);
            // World scale keeps the effect independent of the imported model's scale.
            obj.transform.position = hand.position;
            obj.transform.SetParent(hand, true);
            var ps = obj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(sparks ? .25f : .18f, sparks ? .6f : .42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(sparks ? .9f : .15f, sparks ? 2.3f : .75f);
            main.startSize = new ParticleSystem.MinMaxCurve(sparks ? .018f : .13f, sparks ? .045f : .29f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-.4f, .4f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .28f, .015f), new Color(1f, .85f, .2f));
            main.gravityModifier = sparks ? -.1f : -.035f;
            main.maxParticles = 250;
            var emission = ps.emission;
            emission.rateOverTime = sparks ? 35f : 100f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = sparks ? .08f : .1f;
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(new Color(1f, .95f, .45f), 0f),
                new GradientColorKey(new Color(1f, .35f, .015f), .35f),
                new GradientColorKey(new Color(.65f, .025f, .002f), 1f)
            }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .12f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, .5f), new Keyframe(.2f, 1f), new Keyframe(1f, 0f)));
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = flameMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        private void OnAnimatorMove()
        {
            if (IsActive && locomotion && locomotion.isActiveAndEnabled) locomotion.ApplyAbilityMotion(animator.deltaPosition);
        }

        private void LateUpdate()
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool active = state.IsTag("Attack") && state.normalizedTime >= .04f && state.normalizedTime < .9f;
            if (active != emitting)
            {
                emitting = active;
                SetEmission(leftFlames, active); SetEmission(rightFlames, active);
                SetEmission(leftSparks, active); SetEmission(rightSparks, active);
            }
            float intensity = active ? 2.5f + Mathf.Sin(Time.time * 37f) * .5f : 0f;
            if (leftLight) leftLight.intensity = intensity;
            if (rightLight) rightLight.intensity = intensity;
        }

        private static void SetEmission(ParticleSystem particles, bool active)
        {
            if (!particles) return;
            if (active) particles.Play();
            else particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnDisable()
        {
            emitting = false;
            foreach (var ps in new[] { leftFlames, rightFlames, leftSparks, rightSparks })
                if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (leftLight) leftLight.intensity = 0f;
            if (rightLight) rightLight.intensity = 0f;
        }
    }
}
