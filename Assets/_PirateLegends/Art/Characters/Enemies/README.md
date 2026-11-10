# Enemigos piratas

Origen: `Descargas/EnemigosPiratas/Meshy_AI_assets_20261108_150715`.

- `CorsarioEspadachin`: carpeta original terminada en `f790b038`; nueve animaciones.
- `CorsarioLuchador`: carpeta original terminada en `0f1c2144`; siete animaciones.

Cada carpeta conserva el modelo original en Models, los FBX de animación en Animations y la textura en Textures. Los originales de Descargas no se modificaron.

## Reconstrucción de skinning — 2026-11-10

Los FBX originales de modelo no contienen pesos y los clips contienen exclusivamente esqueletos. La vinculación inicial por calor/distancia deformaba botas y mezclaba influencias de zonas distintas. Se sustituyó por pesos locales a las cadenas anatómicas, con transiciones suaves en tobillos, rodillas, caderas, columna, hombros, codos y muñecas. Suelas y cabeza conservan su forma; los dedos utilizan sus respectivas cadenas para recuperar la articulación de agarre y golpe. Se regularizan los pesos sobre la superficie soldando las costuras UV durante el cálculo; al exportar se conservan geometría, UVs, materiales y esqueleto originales. Máximo cuatro influencias normalizadas por vértice, sin vértices sin pesos.

`Models/*_Rigged.fbx` contiene el resultado. Se reconstruye en una instancia independiente de Blender mediante:

```sh
blender -b --python Tools/Art/prepare_pirate_enemy_rigs.py
```

Estos pesos se reconstruyeron con los archivos disponibles; no se presentan como los pesos originales del servicio Mixamo.

## Escena

Los prefabs `PF_CorsarioEspadachin` y `PF_CorsarioLuchador` están en `SCN_Frostbound_AbilitiesTest`, delante del jugador, en (-2, 0, -1) y (2, 0, -1). Miden aproximadamente 1,85 m, tienen materiales URP y collider. Al pulsar Play recorren todos sus clips automáticamente, con root motion desactivado para permanecer en su sitio. Los controladores `*_Preview.controller` son de exhibición: no incorporan IA, persecución, daño ni armas equipadas.

## Validación

Ambos avatares Humanoid válidos. Se evaluaron ocho fases de cada uno de los 16 clips (128 poses por versión), con vértices finitos y pesos normalizados. Se comparó la longitud de las aristas de ambas suelas a lo largo de todas las poses: el máximo cociente entre longitud mayor y menor pasó de 9,628266 a 1,000163 en el espadachín y de 4,895142 a 1,000216 en el luchador. La variación tras corregirlas es inferior al 0,03 %. Es una medida específica de estabilidad de las suelas, no de toda la deformación corporal.

Revisión visual de dos fases por clip con skinning evaluado explícitamente para evitar capturas de matrices de piel del fotograma anterior. Comparativas en `Captures/Enemies/*_comparison_*.png`: filas por clip, columnas anterior/corregido en fase 0,30 y anterior/corregido en fase 0,65. Informe en `Captures/Enemies/SkinningRepairReport.json`. Consola sin errores; escena fuera de Play al terminar.
