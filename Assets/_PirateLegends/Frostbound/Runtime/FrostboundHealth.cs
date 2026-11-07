using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateLegends.Frostbound
{
    public sealed class FrostboundHealth : MonoBehaviour
    {
        [Min(1)] public float maximum = 200;
        public int team = 2;
        public float Current { get; private set; }
        public int HitCount { get; private set; }
        public Vector3 LastImpact { get; private set; }
        Renderer[] surfaces;
        MaterialPropertyBlock tint;
        float flashUntil;
        void Awake() { surfaces = GetComponentsInChildren<Renderer>(); tint = new MaterialPropertyBlock(); ResetHealth(); }
        public void ResetHealth() { Current = maximum; HitCount = 0; }
        public void Damage(float amount, Vector3 impact)
        {
            if (Current <= 0 || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount); HitCount++; LastImpact = impact; flashUntil = Time.time + .18f;
        }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ResetHealth();
            Color color = Time.time < flashUntil ? Color.white : Current <= 0 ? new Color(.12f,.17f,.2f) : new Color(1f,.36f,.15f);
            tint.SetColor("_BaseColor", color);
            foreach (var surface in surfaces) surface.SetPropertyBlock(tint);
        }
        void OnGUI()
        {
            var camera = Camera.main; if (!camera) return;
            var point = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
            if (point.z <= 0) return;
            float x = point.x - 45, y = Screen.height - point.y;
            GUI.color = new Color(.03f,.06f,.09f,.9f); GUI.DrawTexture(new Rect(x,y,90,35), Texture2D.whiteTexture);
            GUI.color = Current > 0 ? new Color(.2f,.85f,1) : Color.gray;
            GUI.DrawTexture(new Rect(x+3,y+26,84*Current/maximum,5), Texture2D.whiteTexture);
            GUI.color = Color.white; GUI.Label(new Rect(x+5,y+2,85,24), $"{name.Replace("Blanco_","B")}  {Current:0}");
        }

        public static int Area(Vector3 point, float radius, float damage, Transform caster, int casterTeam, bool friendlyFire, LayerMask mask)
        {
            var seen = new HashSet<FrostboundHealth>(); int hitCount = 0;
            foreach (var collider in Physics.OverlapSphere(point, radius, mask, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.IsChildOf(caster)) continue;
                var health = collider.GetComponentInParent<FrostboundHealth>();
                if (!health || !seen.Add(health) || (!friendlyFire && health.team == casterTeam)) continue;
                Vector3 end = collider.bounds.center;
                bool blocked = false;
                foreach (var hit in Physics.RaycastAll(point + Vector3.up*.08f, end-(point+Vector3.up*.08f), Vector3.Distance(point+Vector3.up*.08f,end), mask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(caster) || hit.collider.GetComponentInParent<FrostboundHealth>() == health) continue;
                    blocked = true; break;
                }
                if (!blocked && health.Current > 0) { health.Damage(damage, point); hitCount++; }
            }
            return hitCount;
        }
    }
}
