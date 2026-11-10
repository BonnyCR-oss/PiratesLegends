using PirateLegends.Player;
using UnityEngine;

namespace PirateLegends.World
{
    [DefaultExecutionOrder(-175)]
    public sealed class OceanWeather : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Terrain island;
        [SerializeField] private Renderer ocean;
        [SerializeField] private Light sun;
        [SerializeField] private Transform dockRamp;
        private static readonly int SeaState = Shader.PropertyToID("_SeaState");
        private Vector3 rampStart, rampEnd;
        private float rampThickness;
        private float storm, elapsed, waveHeight;
        private bool stormTarget;
        private float clearSun, clearFog;
        private Color clearFogColor, clearAmbient;
        private Material clearSky, runtimeSky;
        private float clearExposure;

        public float SeaLevel { get; private set; }
        public bool StormIncoming => stormTarget;
        public float Storm => storm;

        private void Awake()
        {
            waveHeight = ocean.sharedMaterial.GetFloat("_WaveHeight");
            clearFog = RenderSettings.fogDensity;
            clearFogColor = RenderSettings.fogColor;
            clearAmbient = RenderSettings.ambientSkyColor;
            if (sun) clearSun = sun.intensity;
            clearSky = RenderSettings.skybox;
            if (clearSky && clearSky.HasProperty("_Exposure"))
            {
                runtimeSky = new Material(clearSky);
                clearExposure = runtimeSky.GetFloat("_Exposure");
                RenderSettings.skybox = runtimeSky;
            }
            if (dockRamp)
            {
                rampStart = dockRamp.TransformPoint(new Vector3(0f, .5f, -.5f));
                rampEnd = dockRamp.TransformPoint(new Vector3(0f, .5f, .5f));
                rampThickness = dockRamp.localScale.y;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if ((input && input.ToggleWeatherPressed) || elapsed >= (stormTarget ? 55f : 110f))
            {
                stormTarget = !stormTarget;
                elapsed = 0f;
            }
            storm = Mathf.MoveTowards(storm, stormTarget ? 1f : 0f, Time.deltaTime / 12f);
            SeaLevel = .13f * Mathf.Sin(Time.time * .024f) + .58f * storm;
            Shader.SetGlobalVector(SeaState, new Vector4(SeaLevel, storm, Time.time, 1f));
            if (sun) sun.intensity = Mathf.Lerp(clearSun, clearSun * .31f, storm);
            RenderSettings.fogDensity = Mathf.Lerp(clearFog, .0045f, storm);
            RenderSettings.fogColor = Color.Lerp(clearFogColor, new Color(.33f, .41f, .46f), storm);
            RenderSettings.ambientSkyColor = Color.Lerp(clearAmbient, clearAmbient * .52f, storm);
            if (runtimeSky) runtimeSky.SetFloat("_Exposure", Mathf.Lerp(clearExposure, clearExposure * .42f, storm));
            if (dockRamp)
            {
                Vector3 end = rampEnd + Vector3.up * SeaLevel;
                Vector3 span = end - rampStart;
                Quaternion rotation = Quaternion.LookRotation(span);
                dockRamp.SetPositionAndRotation((rampStart + end) * .5f - rotation * Vector3.up * (rampThickness * .5f), rotation);
                Vector3 scale = dockRamp.localScale;
                scale.z = span.magnitude;
                dockRamp.localScale = scale;
            }
        }

        public bool TryGetSurface(Vector3 position, out float height)
        {
            height = 0f;
            if (!ocean || !ocean.enabled || !ocean.gameObject.activeInHierarchy) return false;

            Bounds bounds = ocean.bounds;
            if (position.x < bounds.min.x || position.x > bounds.max.x
                || position.z < bounds.min.z || position.z > bounds.max.z) return false;

            height = HeightAt(position);
            return true;
        }

        public float HeightAt(Vector3 position)
        {
            float time = Time.time;
            float swell = Mathf.Sin(position.x * .109f + position.z * .057f - time * .79f);
            float crossing = .48f * Mathf.Sin(position.x * -.097f + position.z * .176f - time * 1.18f);
            float chop = .23f * Mathf.Sin(position.x * .264f + position.z * -.157f - time * 1.62f);
            float seabed = island ? island.SampleHeight(position) + island.transform.position.y : -12f;
            float coast = Mathf.Lerp(.32f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.2f, 5.5f, SeaLevel - seabed)));
            return SeaLevel + (swell + crossing + chop) * waveHeight * Mathf.Lerp(1f, 3.8f, storm) * coast;
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(SeaState, Vector4.zero);
            if (sun) sun.intensity = clearSun;
            RenderSettings.fogDensity = clearFog;
            RenderSettings.fogColor = clearFogColor;
            RenderSettings.ambientSkyColor = clearAmbient;
            if (!runtimeSky) return;
            RenderSettings.skybox = clearSky;
            Destroy(runtimeSky);
        }
    }
}
