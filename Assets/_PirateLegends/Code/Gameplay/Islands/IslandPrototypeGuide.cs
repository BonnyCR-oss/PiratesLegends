using UnityEngine;
using PirateLegends.Player;
using PirateLegends.Ships;
namespace PirateLegends.World
{
    public sealed class IslandPrototypeGuide : MonoBehaviour
    {
        [SerializeField] private Vector3 safePosition;
        [SerializeField] private Terrain island;
        [SerializeField] private PirateShipHelm ship;
        [SerializeField] private OceanWeather weather;
        private PlayerLocomotion motor;
        private GUIStyle title, controls;
        private void Awake()
        {
            motor = GetComponent<PlayerLocomotion>();
            motor.Water = weather;
        }
private void Update()
        {
            Vector3 p = transform.position;
            if (weather)
            {
                if (p.y < -5f && !weather.TryGetSurface(p, out _)) motor.TransferTo(safePosition);
                return;
            }

            bool belowShore = p.y < .15f && (island == null || island.SampleHeight(p) + island.transform.position.y < -.35f);
            if (belowShore || p.y < -5f) motor.TransferTo(safePosition);
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
            GUI.Box(new Rect(18,18,440,weather ? 137 : ship ? 114 : 91), GUIContent.none);
            GUI.Label(new Rect(32,25,340,29), "BAHÍA DEL ALBA", title);
            bool steering = ship && ship.IsSteering;
            string movement = steering ? "W/S · Avance y reversa   A/D · Girar   Espacio · Frenar"
                : motor.IsSwimming ? "WASD · Nadar"
                : "WASD · Mover   Shift · Correr   Espacio · Saltar";
            GUI.Label(new Rect(32,55,420,23), movement, controls);
            if (weather)
            {
                string state = weather.StormIncoming ? (weather.Storm < .9f ? "Tormenta en camino" : "Tormenta") : (weather.Storm > .08f ? "Despejando" : "Mar en calma");
                GUI.Label(new Rect(32,121,420,23), $"{state}   ·   F6 Cambiar clima", controls);
            }
            if (ship)
            {
                string prompt = steering ? $"E · Soltar timón   |   {Mathf.Abs(ship.Speed):0.0} m/s"
                    : ship.CanTakeHelm ? "E · Tomar el timón"
                    : ship.CanUseCabinDoors ? (ship.CabinDoorsOpen ? "E · Cerrar las puertas" : "E · Abrir las puertas")
                    : ship.CanBoard ? "E · Embarcar"
                    : ship.CanDisembark ? "E · Desembarcar"
                    : motor.RidingShip == ship ? "Acércate al timón o a las puertas de la cabina"
                    : "Ve al final del muelle para embarcar con E";
                GUI.Label(new Rect(32,99,420,23), prompt, controls);
            }
            GUI.Label(new Rect(32,77,340,23), "Ratón · Cámara   1 · Fuego   Esc · Soltar cursor", controls);
        }
    }
}