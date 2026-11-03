# Configuración mediante ScriptableObjects

Usar ScriptableObjects para definiciones de diseño compartidas. Guardar las clases C# en el sistema correspondiente de `Assets/_PirateLegends/Code` y sus instancias `.asset` en `Assets/_PirateLegends/Data`.

| Definición prevista | Clase en Code | Instancias en Data | Contenido |
| --- | --- | --- | --- |
| AbilityDefinition | Gameplay/Abilities | Abilities/elemento | Identificador, icono, parámetros base y referencias de presentación |
| WeaponDefinition | Gameplay/Combat | Weapons | Daño, alcance y tiempos configurables |
| SkillTreeDefinition | Gameplay/Progression | Abilities/elemento | Tres ramas, desbloqueos y mejoras numéricas |
| EnemyDefinition | Gameplay/Enemies | Enemies | Estadísticas y configuración de enemigos/jefes |
| AllyDefinition | Gameplay/Allies | Allies | Especialidad y configuración de los cuatro reclutas |
| IslandDefinition | Gameplay/Islands | Islands | Identificador estable, jefe, tienda y recluta asociado |
| ShipDefinition | Gameplay/Ships | Ships | Estadísticas base, puestos y mejoras permitidas |
| ShopDefinition | Gameplay/Economy | Shops | Catálogo y precios |
| ElementUITheme | UI | UIThemes | Colores, sprites y efectos del HUD/árbol por elemento |

Estos nombres describen el diseño previsto; todavía no existen clases ni assets implementados.

## Estado de partida

La salud actual, el daño pendiente del barco, los recursos, las chispas, los desbloqueos y las islas liberadas pertenecen al estado de ejecución y al guardado. No modificar las definiciones compartidas para almacenar ese progreso.

El guardado debe tener versión e identificadores estables, con migraciones cuando cambie su formato. Un cambio de nombre visible no debe cambiar la identidad del contenido.

## Límites entre sistemas

- Combat resuelve daño; Player, Enemies, Allies y Abilities utilizan ese contrato.
- Islands controla ocupación, jefe y liberación; notifica a economía, progresión y presentación.
- UI muestra estado y solicita acciones; las reglas y validaciones viven en los sistemas correspondientes.
- Networking comunica intenciones y replica resultados; la autoridad valida daño, compras y recompensas.
- Bootstrap conecta implementaciones y referencias al iniciar la partida.
- Core contiene únicamente contratos y conceptos compartidos que realmente lo necesiten.
- Editor queda fuera de las compilaciones del jugador mediante un ensamblado exclusivo de editor cuando se introduzcan asmdefs.

Crear pocos ensamblados al inicio y ampliar la división cuando existan dependencias reales. Evitar ciclos entre módulos.
