# Frostbound — prueba controlable

Abrir `Assets/_PirateLegends/Scenes/Tests/SCN_Frostbound_AbilitiesTest.unity` y pulsar Play.

- WASD: caminar. Shift: correr. Espacio: saltar. Ratón: orientar cámara y apuntado.
- 1: Impacto glacial sobre suelo visible seleccionado (anillo cian), alcance 20 m.
- 2: tres lanzas hacia el objetivo seleccionado al comenzar el lanzamiento.
- Mantener 3: invocar arco, tensar y apuntar. Soltar 3: disparar. Un toque breve también dispara tras completar la preparación. Clic durante la pose sostenida también libera la flecha.
- Esc: liberar cursor. Clic: capturarlo. R: restaurar salud de los blancos.

Prefab: `Prefabs/PF_Player_Frostbound.prefab`. Animator: `Animations/AC_Frostbound.controller`.
Reutiliza el visual y Cloth originales, con Avatar Humanoid validado; locomoción mediante CharacterController, root motion desactivado. Siete clips: Idle, Walk, Run, Jump y tres habilidades. H3 sostiene la pose y continúa la animación al disparar. Arco en mano izquierda y cuerda dinámica a mano derecha.

Salud nueva `FrostboundHealth`: no existía un sistema de daño en el proyecto. Daño, alcance, máscaras, equipos, fuego amigo y cooldowns configurables en FrostboundAbilities. H1 fija un punto válido de suelo y aplica área al golpe; lanzas/flecha usan barrido de colisión real, ignoran al lanzador, detonan en geometría y no causan daño al expirar sin impacto. El área deduplica blancos con varios colliders y comprueba obstáculos. Cuatro blancos con salud, destello y reinicio; muro para comprobar cobertura.

VFX recreados en Unity con mallas, fragmentos, anillos, estelas e invocación/disolución por escala. Son una adaptación jugable; los nodos de materiales y VFX completos de Blender no se importan. El arco se disuelve por escala, sin una máscara progresiva desde las puntas.

Verificación en Play Mode mediante eventos reales del Input System: 13 comprobaciones aprobadas (Avatar, caminar, correr, saltar, H1/daño único, H2/tres impactos, H3/pose sostenida, H3/impacto, limpieza del arco, muro, equipo aliado, fallo sin daño, reinicio con R). Informe y capturas: `Captures/Frostbound/PlayModeReport.json` y PNG en la misma carpeta, fuera de Assets. Consola sin errores ni advertencias durante las pruebas. `FrostboundPlayModeCheck` se adjunta solo durante verificación, no está guardado en la escena ni el prefab.

Las escenas y assets anteriores se conservaron. No se hicieron commits ni publicaciones.
