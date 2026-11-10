using UnityEngine;

namespace PirateLegends.Frostbound
{
    // Self-contained presentation lifetime; owns no damage or collision behavior.
    public sealed class FrostboundVfxTransient : MonoBehaviour
    {
        public float lifetime = 1, delay, grow = .06f;
        public bool expand;
        public Vector3 from = Vector3.one, to = Vector3.one;
        float age;
        Renderer[] surfaces;
        LineRenderer line;
        float width;
        Color start, end;
        void Start()
        {
            surfaces = GetComponentsInChildren<Renderer>();
            line = GetComponent<LineRenderer>();
            if (line) { width = line.widthMultiplier; start = line.startColor; end = line.endColor; }
            transform.localScale = from;
            if (delay > 0) foreach (var r in surfaces) r.enabled = false;
        }
        void Update()
        {
            age += Time.deltaTime;
            if (age < delay) return;
            float local = age - delay, t = local / lifetime;
            if (t >= 1) { Destroy(gameObject); return; }
            foreach (var r in surfaces) r.enabled = true;
            float entrance = Mathf.SmoothStep(0, 1, local / Mathf.Max(.001f, grow));
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1, t));
            transform.localScale = (expand ? Vector3.Lerp(from, to, 1 - Mathf.Pow(1 - t, 3)) : to) * entrance;
            if (line)
            {
                line.widthMultiplier = width * entrance;
                line.startColor = new Color(start.r, start.g, start.b, start.a * fade);
                line.endColor = new Color(end.r, end.g, end.b, end.a * fade);
            }
            else transform.localScale *= fade;
        }
    }
}
