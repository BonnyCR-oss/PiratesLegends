# Selector de personajes de prueba

Escena: `Assets/_PirateLegends/Scenes/Tests/SCN_IceCharacter_ClothTest.unity`.

Al pulsar Play aparece un selector con cuatro espacios. Fire, Ice y Viento están disponibles; el cuarto espacio se muestra como Próximamente. Selecciona una tarjeta y pulsa Probar personaje. Esc vuelve al selector y reinicia el personaje en el punto de aparición.

Fire conserva WASD, Shift, Espacio y la habilidad en 1. Ice tiene desplazamiento de prueba con WASD y cámara con ratón usando su caminata actual; no incorpora todavía animaciones de carrera, salto o habilidades. El prefab Frostbound original conserva su configuración: el adaptador de movimiento solo se añade a la instancia de prueba.

Para añadir los próximos personajes, selecciona `Character Test Selector` en la escena y edita `Characters` en el Inspector: nombre, descripción, controles, color y prefab. Una tarjeta se habilita automáticamente al asignarle un prefab. El selector admite ampliar la lista; el diseño actual está preparado para cuatro espacios. Los personajes con PlayerLocomotion conservan ese controlador; los personajes visuales con rig, Animator y SkinnedMeshRenderer reciben el motor básico de caminata durante la prueba.

Archivos de interfaz: `Assets/_PirateLegends/UI/CharacterTest/CharacterTestSelector.uxml` y `.uss`. Lógica: `Code/UI/TestCharacterSelector.cs` y `TestWalkMotor.cs`. Panel escalable: `Settings/CharacterTestPanel.asset`.

## Validación en Unity 6000.5.6f1

Verificados los clics sobre tarjetas y Probar personaje, selección de Fire/Ice, dos tarjetas pendientes deshabilitadas, desplazamiento de ambos personajes con entrada WASD y regreso mediante Esc con cursor liberado. El motor de Fire y el adaptador de Ice se habilitan únicamente durante la prueba. Consola final sin errores ni advertencias. Captura: `Captures/CharacterSelector_Ice_Verified.png`.

## Viento

Viento ocupa el tercer espacio y utiliza `PF_Player_Wind.prefab`, con ropa dinámica. Controles: WASD, Shift, Espacio, ratón; 1 y 2 para los dos movimientos especiales, 3 para muerte y 0 para revivir. Esc vuelve al selector. Detalles en `Assets/_PirateLegends/Art/Characters/greenCharacter/README.md`.
