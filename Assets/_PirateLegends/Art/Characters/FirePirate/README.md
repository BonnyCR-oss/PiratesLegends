# Pirata de fuego

Personaje de fuego de los cuatro elementos del GDD. Prefab: Assets/_PirateLegends/Prefabs/Characters/PF_Player_Fire.prefab.

## Archivos

- Models/CHR_FirePirate.fbx: modelo con esqueleto y animación Idle, procedente de Iddle.fbx.
- Animations/ANIM_FirePirate_Walk.fbx: Walking_withSkin.
- Animations/ANIM_FirePirate_Run.fbx: Running_withSkin.
- Animations/ANIM_FirePirate_Sprint.fbx: RunFast_withSkin.
- Animations/ANIM_FirePirate_Jump.fbx: Jump_Over_Obstacle_2_withSkin, aprobado para el salto normal.
- Animations/AC_FirePirate.controller: locomoción y salto.
- Textures/T_FirePirate_BaseColor, Normal, Metallic, Roughness: mapas originales renombrados.
- Textures/T_FirePirate_MetallicSmoothness.png: metálico en rojo y suavidad (1-rugosidad) en alfa para URP.
- Materials/MAT_FirePirate.mat: material URP Lit.

Los FBX de animación conservan sus mallas de origen, pero el prefab solo utiliza un modelo visual. El modelo sin rig original queda archivado en SourceAssets/Characters/FirePirate/CHR_FirePirate_SourceMesh.fbx. Los originales externos no se modificaron. ImportNames.json contiene la correspondencia de nombres.

## Configuración

Avatar Humanoid válido, compartido por los clips. Idle, Walk, Run y Sprint en bucle; Jump de 0,88 s sin bucle. Root motion desactivado: el motor del jugador controlará el desplazamiento físico.

Animator: parámetro float Speed, con 0=Idle, 1=Walk, 2=Run y 3=Sprint; trigger Jump para saltar y volver a locomoción. Speed expresa el índice de locomoción, no metros por segundo; el futuro motor debe convertir su velocidad a este parámetro.

Prefab: raíz Player con CharacterController, hijo Visual con Animator y CameraTarget para la futura cámara. Colocado en SCN_GameplayTest; la cápsula provisional queda desactivada.

Se validaron poses de los cinco clips en una instancia temporal y se verificó visualmente el material. El personaje incluye PlayerInputReader y PlayerLocomotion: WASD para caminar, Shift mantenido para correr y Espacio para saltar. La escena de prueba incluye ThirdPersonOrbitCamera con giro de ratón. Consultar Documentation/Production/Controles_basicos.md.


## Animaciones activas actualizadas

ANIM_FirePirate_Run2.fbx sustituye Run y Sprint en el Blend Tree (incluida la carrera al mantener Shift). ANIM_FirePirate_Jump2.fbx sustituye Jump. Los clips anteriores se conservan como alternativas.

Las nuevas animaciones usan Create From This Model con Avatares Humanoid propios; copiar el Avatar del modelo original produce una incompatibilidad de jerarquía. Unity realiza la adaptación humanoide al personaje. Run2 está configurada en bucle; Jump2 no. Root motion permanece desactivado. Se verificó que los estados reproducen ANIM_FirePirate_Run2 y ANIM_FirePirate_Jump2 con peso 1.

## Modelo activo de Mixamo y corrección de pies

El prefab ahora utiliza Models/CHR_FirePirate_Mixamo.fbx, importado desde FirePiratePoseT.fbx. Run2 y Jump2 comparten exactamente su Avatar (Copy From Other). La altura visual se normalizó a 1,94 m conservando el controlador físico y los controles.

El FBX nuevo no tenía UV. Se recuperaron desde CHR_FirePirate_TextureReference.fbx, copia del modelo original: coincidencia geométrica de los 18.522 triángulos tras normalizar sus límites, error máximo de vértice normalizado 0,00000359. Se generó MSH_FirePirate_Mixamo_Textured.asset con UV por esquina, conservando posiciones, normales, pesos de huesos y bind poses de Mixamo; tangentes recalculadas. El prefab usa esta malla corregida y el material URP existente. Conservar ambos FBX de referencia y la malla generada; volver a importar el FBX de Mixamo no recupera sus UV automáticamente.

Se compararon visualmente caminata, dos fases de carrera y salto con el nuevo rig. La vista con texturas y los pies se verificó en Captures/Feet_MixamoRig_Textured-1.png. Las animaciones originales de Meshy se conservan y se adaptan al nuevo Avatar del personaje.

La prueba final de controles pasó: WASD en cuatro direcciones, carrera de 2,50 m frente a 1,37 m caminando en igual intervalo, salto de 1,24 m con aterrizaje y giro de cámara de 19,2 grados. Consola final sin errores ni advertencias. Escena guardada fuera de Play.
