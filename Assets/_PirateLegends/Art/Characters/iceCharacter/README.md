# iceCharacter — Frostbound

Importado desde Descargas/characterIce/Preparado_Mixamo/Unity_Ropa/Frostbound_Para_Unity.zip. Los originales de Blender y el ZIP permanecen en su ubicación original.

## Organización

- Models/Frostbound_Unity_Cloth.fbx: cuerpo, faldones, rig Humanoid y caminata incluida.
- Textures/Frostbound_BaseColor.png: textura original.
- Materials/Frostbound_Material.mat: material URP Lit de dos caras.
- Animations/Frostbound_Walking.controller: caminata en bucle.
- Prefab: Assets/_PirateLegends/Prefabs/Characters/Frostbound_Con_Ropa.prefab.
- Configurador: Assets/_PirateLegends/Code/Editor/FrostboundClothSetup.cs.
- Escena: Assets/_PirateLegends/Scenes/Tests/SCN_IceCharacter_ClothTest.unity.

## Regenerar

Seleccionar Models/Frostbound_Unity_Cloth.fbx y ejecutar Tools > Frostbound > Crear prefab con ropa. El configurador actualiza el material, controlador y prefab en las carpetas anteriores. Mantener los colores de vértices y el esqueleto sin optimizar: codifican las restricciones de Cloth.

Se adaptaron las rutas al orden del proyecto y la conversión de coordenadas de BakeMesh a Cloth: la escala importada del FBX ya está incluida en los vértices horneados y no debe aplicarse otra vez. La asociación geométrica verificada tuvo un error máximo de 0,000000263 m.

## Validación en Unity 6000.5.6f1 — 2026-11-05

Avatar Humanoid válido. Cloth contiene 1.380 partículas: 202 ancladas y 1.178 libres, con libertad gradual hasta 0,22 m. Cuatro cápsulas de piernas registradas en Cloth. El prefab utiliza componentes nativos, sin scripts propios de ejecución.

Prueba de caminata en Play durante 8 segundos, con 140 fotogramas muestreados: cero posiciones no finitas; variación máxima de los anclajes respecto a la primera muestra de 0,000000363 m; movimiento máximo de las partículas libres entre poses de 0,293 m. Esta última medida compara dos poses distintas, no el límite de alejamiento de la pose animada. Radio máximo de las partículas en el espacio de Cloth: 0,883 m. Animación activa y consola final sin errores ni advertencias. Revisión visual en Captures/IceCharacter_Cloth_Runtime.png y Captures/IceCharacter_Cloth_Close.png.

Abrir la escena de prueba y pulsar Play para ver el selector Fire/Ice. Elegir Ice y pulsar Probar personaje permite caminar con WASD y girar la cámara con ratón; Esc vuelve al selector. Apply Root Motion está activado, tal como lo define el paquete original. El prefab original sigue siendo visual; la escena del selector añade un motor de caminata a su instancia de prueba. Consultar Documentation/Production/Selector_de_personajes.md. La validación cubre esta caminata; quedan por ajustar penetraciones y estabilidad al incorporar carrera, salto y otras animaciones.
