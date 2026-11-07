using UnityEngine;

namespace PirateLegends.Frostbound
{
    public sealed class FrostboundProjectile : MonoBehaviour
    {
        public FrostboundAbilities owner;
        public Vector3 direction;
        public float speed = 22, radius = .12f, damage = 30, blastRadius = 1.4f, lifetime = 5;
        public LayerMask collisionMask = ~0;
        public bool arrow;
        public float tipOffset = .9f;
        float age;
        bool impacted;
        public Vector3 ImpactPoint { get; private set; }
        void FixedUpdate()
        {
            if (impacted || !owner) { Destroy(gameObject); return; }
            float distance = speed * Time.fixedDeltaTime;
            Vector3 tip = transform.position + direction * tipOffset;
            var hits = Physics.SphereCastAll(tip, radius, direction, distance, collisionMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(owner.transform)) continue;
                ImpactPoint = hit.point; impacted = true;
                owner.Impact(hit.point + hit.normal*.08f, blastRadius, damage, arrow);
                Destroy(gameObject); return;
            }
            // Handle spawning already overlapping a wall/enemy.
            foreach (var collider in Physics.OverlapSphere(tip, radius, collisionMask, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.IsChildOf(owner.transform)) continue;
                ImpactPoint = collider.ClosestPoint(tip); impacted = true;
                owner.Impact(ImpactPoint, blastRadius, damage, arrow); Destroy(gameObject); return;
            }
            transform.position += direction * distance;
            age += Time.fixedDeltaTime;
            if (age >= lifetime) Destroy(gameObject); // Misses expire without applying damage.
        }
    }
}
