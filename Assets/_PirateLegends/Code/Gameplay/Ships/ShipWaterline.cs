using UnityEngine;

namespace PirateLegends.Ships
{
    [ExecuteAlways]
    [RequireComponent(typeof(PirateShipHelm))]
    public sealed class ShipWaterline : MonoBehaviour
    {
        [SerializeField] private Renderer ocean;
        [SerializeField] private Vector2 hullLength;
        [SerializeField] private Vector4[] hullWidths = new Vector4[17];

        private static readonly int HullTransform = Shader.PropertyToID("_HullWorldToLocal");
        private static readonly int HullRange = Shader.PropertyToID("_HullRange");
        private static readonly int HullWidths = Shader.PropertyToID("_HullWidths");
        private MaterialPropertyBlock properties;
        private PirateShipHelm helm;

        private void OnEnable()
        {
            properties = new MaterialPropertyBlock();
            helm = GetComponent<PirateShipHelm>();
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (!ocean || properties == null || hullWidths.Length != 17) return;
            ocean.GetPropertyBlock(properties);
            properties.SetMatrix(HullTransform, transform.worldToLocalMatrix);
            properties.SetVector(HullRange, new Vector4(hullLength.x, hullLength.y, 1f,
                Application.IsPlaying(gameObject) ? Mathf.Clamp(helm.Speed / 7f, -1f, 1f) : 0f));
            properties.SetVectorArray(HullWidths, hullWidths);
            ocean.SetPropertyBlock(properties);
        }

        private void OnDisable()
        {
            if (!ocean || properties == null) return;
            ocean.GetPropertyBlock(properties);
            properties.SetVector(HullRange, Vector4.zero);
            ocean.SetPropertyBlock(properties);
        }
    }
}
