using UnityEngine;

namespace PirateLegends.Frostbound
{
    // Visual-only effect: gameplay damage is applied by the caster or swept projectile.
    public sealed class FrostboundEffect : MonoBehaviour
    {
        public float lifetime = 1.7f;
        public bool fragment;
        public Vector3 velocity;
        Vector3 size;
        float age;
        void Start() { size = transform.localScale; }
        void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime) { Destroy(gameObject); return; }
            float t = age / lifetime;
            if (fragment)
            {
                velocity += Vector3.down * 3 * Time.deltaTime;
                transform.position += velocity * Time.deltaTime;
                transform.Rotate(new Vector3(70,110,35)*Time.deltaTime);
            }
            float grow = fragment ? 1 : Mathf.SmoothStep(0,1,Mathf.Min(1,age/.16f));
            transform.localScale = size * grow * (1 - Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,t)));
        }
    }
}
