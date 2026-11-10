using System.Collections;
using System.Collections.Generic;
using PirateLegends.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace PirateLegends.Frostbound
{
    [DefaultExecutionOrder(100)]
    public sealed class FrostboundVfx : MonoBehaviour
    {
        public Shader effectShader;
        public AudioClip chargeSound, slamSound, lanceSound, bowSound, releaseSound, burstSound;
        [Range(0, 1)] public float volume = .65f;
        public bool cameraImpulses = true;
        FrostboundAbilities owner;
        Material energy, mote, mist;
        Transform left, right;
        GameObject channel;
        readonly GameObject[] orbitLances = new GameObject[3];
        readonly List<GameObject> effects = new List<GameObject>();
        LineRenderer handLeft, handRight;
        ParticleSystem leftMotes, rightMotes;
        int kind;
        float started, nextPrune;
        public Material EnergyMaterial => energy;

        public void Configure(FrostboundAbilities caster, Transform leftHand, Transform rightHand)
        {
            owner = caster; left = leftHand; right = rightHand;
            if (energy) return;
            if (!effectShader) effectShader = Shader.Find("Pirate Legends/Frostbound/Energy");
            if (!effectShader) return;
            energy = new Material(effectShader) { name = "Frostbound_Energy_Runtime" };
            energy.SetColor("_Tint", new Color(.25f, .9f, 1.5f, 1));
            mote = new Material(energy); mote.SetFloat("_Shape", 1);
            mist = new Material(mote); mist.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mist.SetColor("_Tint", new Color(.32f, .65f, .8f, .16f));
        }
        GameObject New(string name)
        {
            var go = new GameObject("FrostVFX_" + name); effects.Add(go); return go;
        }
        void Update()
        {
            if (Time.time > nextPrune) { effects.RemoveAll(e => !e); nextPrune = Time.time + 2; }
        }
        void LateUpdate()
        {
            if (!channel || !owner) return;
            if (handLeft && left) handLeft.transform.SetPositionAndRotation(left.position, left.rotation * Quaternion.Euler(90, Time.time * 120, 0));
            if (handRight && right) handRight.transform.SetPositionAndRotation(right.position, right.rotation * Quaternion.Euler(90, -Time.time * 140, 0));
            float power = Mathf.SmoothStep(.7f, 1.25f, (Time.time - started) / 2.5f);
            if (handLeft) handLeft.transform.localScale = Vector3.one * power;
            if (handRight) handRight.transform.localScale = Vector3.one * power;
            if (leftMotes && left) leftMotes.transform.position = left.position;
            if (rightMotes && right) rightMotes.transform.position = right.position;
            if (kind == 2)
            {
                float age = Time.time - started;
                for (int i = 0; i < 3; i++)
                {
                    var lance = orbitLances[i]; if (!lance) continue;
                    float angle = age * 2.4f + i * Mathf.PI * 2 / 3;
                    Vector3 position = transform.position + Vector3.up * (1.45f + Mathf.Sin(angle) * .33f)
                        + transform.right * Mathf.Cos(angle) * .82f - transform.forward * .24f;
                    lance.transform.SetPositionAndRotation(position, Quaternion.LookRotation(owner.AimPoint - position));
                    lance.transform.localScale = Vector3.one * Mathf.SmoothStep(.02f, .7f, age / .55f);
                }
            }
        }
        public void Begin(int number, Vector3 target)
        {
            StopCharge();
            if (!energy) return;
            kind = number; started = Time.time;
            channel = New("Channel_" + number);
            handLeft = Ring("LeftHandRune", Vector3.zero, .15f, .022f, channel.transform);
            handRight = Ring("RightHandRune", Vector3.zero, .12f, .019f, channel.transform);
            leftMotes = Particles("LeftHandFrost", Vector3.zero, 0, true, false, channel.transform);
            rightMotes = Particles("RightHandFrost", Vector3.zero, 0, true, false, channel.transform);
            Sound(number == 3 ? bowSound : chargeSound, transform.position + Vector3.up, .35f, channel.transform);
            if (number == 1)
            {
                var rune = Ring("SlamTargetRune", target + Vector3.up * .045f, owner.groundRadius * .94f, .028f, channel.transform);
                rune.startColor = rune.endColor = new Color(.3f, .8f, 1, .55f);
            }
            if (number == 2)
                for (int i = 0; i < 3; i++)
                {
                    orbitLances[i] = Instantiate(owner.lancePrefab, channel.transform);
                    orbitLances[i].name = "FrostVFX_OrbitLance_" + i;
                    orbitLances[i].transform.localScale = Vector3.one * .02f;
                    MakeVisualOnly(orbitLances[i]);
                    var halo = Ring("LanceHalo", Vector3.zero, .16f, .025f, orbitLances[i].transform);
                    halo.transform.localPosition = Vector3.zero;
                    halo.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
        }
        public Vector3 TakeLance(int index, Vector3 fallback)
        {
            var visual = orbitLances[index];
            if (!visual) return fallback;
            Vector3 result = visual.transform.position;
            Destroy(visual); orbitLances[index] = null;
            Pulse(result, .32f, .25f, Quaternion.Euler(90, 0, 0));
            return result;
        }
        public void StopCharge()
        {
            if (channel) Destroy(channel);
            channel = null; handLeft = handRight = null; leftMotes = rightMotes = null;
            for (int i = 0; i < 3; i++) orbitLances[i] = null;
        }
        public void Nock(GameObject arrow)
        {
            if (!arrow || !energy) return;
            var motes = Particles("ArrowCharge", arrow.transform.position, 0, true, false, arrow.transform);
            motes.transform.localPosition = Vector3.zero;
            var halo = Ring("ArrowChargeHalo", arrow.transform.position, .11f, .025f, arrow.transform);
            halo.transform.localPosition = Vector3.zero;
            halo.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        public void Projectile(GameObject projectile, bool arrow)
        {
            if (!energy) return;
            var trail = projectile.GetComponent<TrailRenderer>();
            if (!trail) trail = projectile.AddComponent<TrailRenderer>();
            trail.sharedMaterial = energy; trail.time = arrow ? .36f : .24f;
            trail.minVertexDistance = .025f; trail.startWidth = arrow ? .10f : .075f; trail.endWidth = 0;
            trail.startColor = new Color(.4f, .9f, 1, .9f); trail.endColor = new Color(.08f, .4f, .9f, 0);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            var frost = Particles("ProjectileFrost", projectile.transform.position, 0, true, false, projectile.transform);
            frost.transform.localPosition = Vector3.zero;
            Sound(arrow ? releaseSound : lanceSound, projectile.transform.position, arrow ? .65f : .4f);
            if (arrow)
            {
                Vector3 direction = projectile.transform.forward;
                Pulse(projectile.transform.position, .65f, .3f, Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0));
                Impulse(projectile.transform.position, .027f, .16f);
                StopCharge();
            }
        }
        public void Impact(Vector3 point, float radius, int ability)
        {
            if (!energy) return;
            StartCoroutine(ImpactSequence(point, radius, ability));
        }
        IEnumerator ImpactSequence(Vector3 point, float radius, int ability)
        {
            bool slam = ability == 1, arrow = ability == 3;
            if (slam) StopCharge();
            Sound(slam ? slamSound : burstSound, point, slam ? .85f : arrow ? .7f : .4f);
            Impulse(point, slam ? .065f : arrow ? .045f : .012f, slam ? .3f : .18f);
            Pulse(point + Vector3.up * .045f, radius, .6f, Quaternion.identity);
            if (slam || arrow) Pulse(point + Vector3.up * .07f, radius * 1.12f, .85f, Quaternion.identity);
            Particles("ImpactSnow", point + Vector3.up * .25f, slam ? 80 : arrow ? 65 : 28, false, false);
            Particles("ColdVapor", point + Vector3.up * .15f, slam ? 20 : 12, false, true);
            for (int i = 0; i < (slam || arrow ? 18 : 8); i++)
            {
                var fragment = Instantiate(owner.fragmentPrefab, point + Vector3.up * .35f, Random.rotation);
                effects.Add(fragment); fragment.name = "FrostVFX_CrystalDebris"; MakeVisualOnly(fragment);
                fragment.transform.localScale *= Random.Range(.1f, .24f);
                var debris = fragment.AddComponent<FrostboundEffect>(); debris.fragment = true; debris.lifetime = .85f;
                debris.velocity = Random.onUnitSphere * 2.4f + Vector3.up * 2;
            }
            var frosted = new HashSet<Transform>();
            foreach (var hit in Physics.OverlapSphere(point, radius, owner.collisionMask, QueryTriggerInteraction.Ignore))
            {
                var health = hit.GetComponentInParent<FrostboundHealth>();
                if (!health || hit.transform.IsChildOf(transform) || !frosted.Add(health.transform)) continue;
                Particles("TargetRime", hit.bounds.center, 12, false, true, health.transform);
            }
            var lightObject = New("ImpactFlash"); lightObject.transform.position = point + Vector3.up * .7f;
            var flash = lightObject.AddComponent<Light>(); flash.color = new Color(.2f, .7f, 1); flash.intensity = 2.8f; flash.range = radius * 1.8f; flash.shadows = LightShadows.None;
            Destroy(lightObject, .1f);
            if (slam || arrow) Cracks(point, radius, slam ? 9 : 6);
            int count = slam ? 14 : arrow ? 10 : 4;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2 * Mathf.PI / count;
                float distance = radius * (i % 2 == 0 ? .48f : .8f);
                Vector3 position = point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                // Place decorative spikes on the floor; airborne impacts remain airborne bursts.
                if (Physics.Raycast(position + Vector3.up * 1.2f, Vector3.down, out var ground, 2f, owner.collisionMask, QueryTriggerInteraction.Ignore)
                    && ground.normal.y > .65f && !ground.collider.transform.IsChildOf(transform))
                {
                    var spike = Instantiate(owner.spikePrefab, ground.point, Quaternion.Euler(Random.Range(-10, 10), angle * Mathf.Rad2Deg, Random.Range(-10, 10)));
                    effects.Add(spike); spike.name = "FrostVFX_RisingCrystal"; MakeVisualOnly(spike);
                    var life = spike.AddComponent<FrostboundVfxTransient>(); life.lifetime = slam ? 2.5f : 1.6f;
                    life.grow = .12f; life.from = Vector3.one * .02f;
                    life.to = spike.transform.localScale * (slam ? Random.Range(.65f, 1.2f) : Random.Range(.35f, .8f));
                }
                if (i % 2 == 1) yield return new WaitForSeconds(slam ? .035f : .018f);
            }
        }
        void Cracks(Vector3 point, float radius, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2 * Mathf.PI / count;
                Vector3 axis = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 side = Vector3.Cross(axis, Vector3.up);
                var go = New("GlowingFissure"); go.transform.position = point + Vector3.up * .05f;
                var line = Line(go, .045f); line.positionCount = 7;
                for (int k = 0; k < 7; k++)
                    line.SetPosition(k, axis * radius * k / 6f + side * (k == 0 ? 0 : Random.Range(-.12f, .12f)));
                var life = go.AddComponent<FrostboundVfxTransient>(); life.lifetime = 1.65f; life.delay = i * .015f; life.grow = .18f;
                if (i % 2 == 0)
                {
                    var branch = New("FissureBranch"); branch.transform.position = go.transform.position;
                    var small = Line(branch, .022f); small.positionCount = 3;
                    Vector3 start = axis * radius * .48f;
                    small.SetPosition(0, start); small.SetPosition(1, start + axis * .25f + side * .3f); small.SetPosition(2, start + axis * .5f + side * .5f);
                    branch.AddComponent<FrostboundVfxTransient>().lifetime = 1.4f;
                }
            }
        }
        LineRenderer Line(GameObject go, float width)
        {
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = energy;
            line.useWorldSpace = false; line.widthMultiplier = width; line.numCornerVertices = 2; line.numCapVertices = 2;
            line.startColor = new Color(.55f, .95f, 1, 1); line.endColor = new Color(.12f, .55f, 1, .6f);
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }
        LineRenderer Ring(string name, Vector3 position, float radius, float width, Transform parent = null)
        {
            var go = New(name); go.transform.position = position;
            if (parent) go.transform.SetParent(parent, true);
            go.transform.localScale = Vector3.one;
            var line = Line(go, width); line.loop = true; line.positionCount = 65;
            for (int i = 0; i < 65; i++)
            {
                float angle = 2 * Mathf.PI * i / 64;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
            }
            return line;
        }
        void Pulse(Vector3 point, float radius, float duration, Quaternion rotation)
        {
            var ring = Ring("Shockwave", point, 1, .075f);
            ring.transform.rotation = rotation;
            var life = ring.gameObject.AddComponent<FrostboundVfxTransient>(); life.lifetime = duration; life.expand = true;
            life.from = Vector3.one * .05f; life.to = Vector3.one * radius;
        }
        ParticleSystem Particles(string name, Vector3 position, int burst, bool continuous, bool vapor, Transform parent = null)
        {
            var go = New(name); go.transform.position = position;
            if (parent) go.transform.SetParent(parent, true);
            var system = go.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.loop = continuous; main.duration = 1;
            main.startLifetime = vapor ? new ParticleSystem.MinMaxCurve(.6f, 1.2f) : new ParticleSystem.MinMaxCurve(.2f, .65f);
            main.startSpeed = vapor ? new ParticleSystem.MinMaxCurve(.12f, .55f) : continuous ? new ParticleSystem.MinMaxCurve(.04f, .35f) : new ParticleSystem.MinMaxCurve(1.2f, 4);
            main.startSize = vapor ? new ParticleSystem.MinMaxCurve(.3f, .8f) : new ParticleSystem.MinMaxCurve(.025f, .07f);
            main.startColor = vapor ? Color.white : new Color(.65f, .95f, 1, .8f);
            main.gravityModifier = continuous || vapor ? -.02f : .25f; main.maxParticles = continuous ? 50 : 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.playOnAwake = false;
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = vapor ? .5f : continuous ? .09f : .18f;
            var emission = system.emission; emission.enabled = continuous; emission.rateOverTime = continuous ? 24 : 0;
            var colors = system.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.2f, .6f, 1), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(vapor ? .3f : .8f, .12f), new GradientAlphaKey(0, 1) });
            colors.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = vapor ? mist : mote;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            system.Play(); if (burst > 0) system.Emit(burst);
            if (!continuous) Destroy(go, 1.5f);
            return system;
        }
        void Sound(AudioClip clip, Vector3 position, float gain, Transform parent = null)
        {
            if (!clip || volume <= 0) return;
            var go = New("IceSound"); go.transform.position = position;
            if (parent) go.transform.SetParent(parent, true);
            var source = go.AddComponent<AudioSource>(); source.clip = clip; source.spatialBlend = .85f;
            source.volume = volume * gain; source.minDistance = 3; source.maxDistance = 32;
            source.rolloffMode = AudioRolloffMode.Linear; source.Play(); Destroy(go, clip.length + .1f);
        }
        void Impulse(Vector3 point, float amplitude, float duration)
        {
            if (!cameraImpulses || !owner.aimCamera) return;
            var orbit = owner.aimCamera.GetComponent<ThirdPersonOrbitCamera>();
            if (orbit) orbit.AddImpulse(amplitude / (1 + Vector3.Distance(owner.aimCamera.transform.position, point) * .075f), duration);
        }
        static void MakeVisualOnly(GameObject visual)
        {
            foreach (var c in visual.GetComponentsInChildren<Collider>()) c.enabled = false;
        }
        void OnDisable()
        {
            StopAllCoroutines(); StopCharge();
            foreach (var effect in effects) if (effect) Destroy(effect);
            effects.Clear();
        }
        void OnDestroy() { if (energy) Destroy(energy); if (mote) Destroy(mote); if (mist) Destroy(mist); }
    }
}
