# Personaje de viento

Prefab jugable: `Assets/_PirateLegends/Prefabs/Characters/PF_Player_Wind.prefab`.
Escena con selector: `Assets/_PirateLegends/Scenes/Tests/SCN_IceCharacter_ClothTest.unity`.
Pulsa Play, selecciona Viento y Probar personaje.

- WASD: caminar; Shift: correr; ratón: cámara; Espacio: saltar.
- 1 y 2 (también teclado numérico): Hability1 y Hability2.
- 3: probar animación de muerte; 0: revivir; Esc: volver al selector.

Las habilidades se ejecutan en el suelo, bloquean el desplazamiento durante su animación y regresan a locomoción. Son movimientos especiales de prueba; todavía no aplican daño ni efectos de viento. La muerte conserva la pose final hasta revivir o volver al selector.

`Animations/` contiene los siete FBX de animación originales y `AC_Wind.controller`, con mezcla de reposo/caminata/carrera. Los clips se importan como humanoides para retargeting al avatar de Verde_Unity_Cloth; locomoción y salto son controlados por CharacterController, sin movimiento de raíz.

`Models/` conserva los dos FBX originales nuevos y el modelo con ropa ya preparado. El prefab jugable anida Verde_Con_Ropa para conservar material, textura, tres simulaciones Cloth y sus ocho colisionadores. `Source~/` conserva Blender y los archivos de Unity_Ropa; Unity ignora esa carpeta de fuentes para evitar importar Blender automáticamente.

Configuración de editor: `WindCharacterSetup.cs`. Lógica de habilidades: `WindAbilities.cs`. Se reutilizan PlayerInputReader y PlayerLocomotion; MovementLocked permite bloquear el movimiento durante habilidades y muerte.
