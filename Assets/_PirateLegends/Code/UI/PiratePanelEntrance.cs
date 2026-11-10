using UnityEngine;

namespace PirateLegends.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PiratePanelEntrance : MonoBehaviour
    {
        private const float Duration = .22f;
        private RectTransform panel;
        private CanvasGroup group;
        private Vector2 rest;
        private float elapsed;

        private void Awake()
        {
            panel = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            rest = panel.anchoredPosition;
        }

        private void OnEnable()
        {
            if (PlayerPrefs.GetInt("Pirates.Menu.Motion", 0) != 0)
            {
                Finish();
                return;
            }

            elapsed = 0;
            group.alpha = 0;
            group.interactable = false;
            panel.anchoredPosition = rest + Vector2.right * 18f;
        }

        private void Update()
        {
            if (elapsed >= Duration)
                return;

            elapsed += Time.unscaledDeltaTime;
            float remaining = 1f - Mathf.Clamp01(elapsed / Duration);
            float offset = remaining * remaining * remaining;
            group.alpha = 1f - offset;
            panel.anchoredPosition = rest + Vector2.right * (offset * 18f);
            if (elapsed >= Duration)
                Finish();
        }

        private void OnDisable() => Finish();

        private void Finish()
        {
            if (!group)
                return;

            elapsed = Duration;
            group.alpha = 1f;
            group.interactable = true;
            panel.anchoredPosition = rest;
        }
    }
}