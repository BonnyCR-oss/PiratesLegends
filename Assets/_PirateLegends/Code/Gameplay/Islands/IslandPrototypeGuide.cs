using UnityEngine;
namespace PirateLegends.World
{
    public sealed class IslandPrototypeGuide : MonoBehaviour
    {
        [SerializeField] private Vector3 safePosition;
        [SerializeField] private Terrain island;
        private CharacterController body;
        private GUIStyle title, controls;
        private void Awake() { body = GetComponent<CharacterController>(); }
        private void Update()
        {
            Vector3 p = transform.position;
            bool inWater = p.y < .15f && (island == null || island.SampleHeight(p) + island.transform.position.y < -.35f);
            if (inWater || p.y < -5f)
            {
                body.enabled = false;
                transform.position = safePosition;
                body.enabled = true;
            }
        }
        private void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(1f,.93f,.76f);
                controls = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                controls.normal.textColor = new Color(.86f,.93f,.96f);
            }
            GUI.Box(new Rect(18,18,365,91), GUIContent.none);
            GUI.Label(new Rect(32,25,340,29), "BAHÍA DEL ALBA", title);
            GUI.Label(new Rect(32,55,340,23), "WASD · Mover   Shift · Correr   Espacio · Saltar", controls);
            GUI.Label(new Rect(32,77,340,23), "Ratón · Cámara   1 · Fuego   Esc · Soltar cursor", controls);
        }
    }
}