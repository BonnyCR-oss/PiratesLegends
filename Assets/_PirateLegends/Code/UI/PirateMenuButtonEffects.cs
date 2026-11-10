using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PirateLegends.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class PirateMenuButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        [SerializeField] private bool leadButton;
        [SerializeField] private Image topGlow;
        [SerializeField] private Image bottomGlow;
        [SerializeField] private RectTransform glint;
        private Button button;
        private RectTransform rect;
        private bool hovered, pressed;
        private float submitTime;
        private float focus;

        private void Awake()
        {
            rect = (RectTransform)transform;
            button = GetComponent<Button>();
        }

        private void Update()
        {
            bool selected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
            bool highlighted = button.IsInteractable() && (hovered || selected);
            float time = Time.unscaledTime;
            focus = Mathf.MoveTowards(focus, highlighted ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            float target = pressed || time < submitTime ? .975f : 1f + focus * .022f;
            rect.localScale = Vector3.one * Mathf.Lerp(rect.localScale.x, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 17f));
            float pulse = leadButton ? .20f + .13f * Mathf.Sin(time * 2f) : .03f;
            var glow = new Color(1f, .70f, .31f, Mathf.Clamp01(pulse + focus * .65f));
            topGlow.color = bottomGlow.color = glow;
            glint.gameObject.SetActive(highlighted);
            if (highlighted)
            {
                float halfWidth = rect.rect.width * .5f;
                float position = Mathf.Repeat(time * .72f, 1f);
                glint.anchoredPosition = new Vector2(Mathf.Lerp(-halfWidth + 19f, halfWidth - 19f, position), 0f);
            }
        }

        private void OnDisable()
        {
            hovered = pressed = false;
            focus = submitTime = 0f;
            if (rect)
                rect.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData _) => hovered = true;
        public void OnPointerExit(PointerEventData _) { hovered = false; pressed = false; }
        public void OnPointerDown(PointerEventData data) => pressed = button.IsInteractable() && data.button == PointerEventData.InputButton.Left;
        public void OnPointerUp(PointerEventData _) => pressed = false;
        public void OnSubmit(BaseEventData _) => submitTime = Time.unscaledTime + .14f;
    }
}