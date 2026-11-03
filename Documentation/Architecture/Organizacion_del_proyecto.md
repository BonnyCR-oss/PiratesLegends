# Organización del proyecto activo

Proyecto Unity: `D:\PiratesLegends`. Editor: Unity 6000.5.6f1, URP.

## Ubicación de los archivos

- `Assets/_PirateLegends/Code`: código propio por sistema; Gameplay contiene Player, Camera, Interaction, Combat, Abilities, Progression, Enemies, Allies, Islands, Ships y Economy.
- `Assets/_PirateLegends/Data`: instancias ScriptableObject para configuración; el progreso actual se almacena por separado.
- `Assets/_PirateLegends/Prefabs`: personajes, armas, habilidades, barcos, entorno y UI reutilizables.
- `Assets/_PirateLegends/Scenes`: Bootstrap, Menus, Ocean, Islands y Tests.
- `Assets/_PirateLegends/Art` y `Audio`: recursos exportados usados por Unity.
- `Assets/_PirateLegends/Settings`: ajustes propios. Los ajustes originales de la plantilla permanecen en `Assets/Settings` hasta que sea necesario reorganizarlos desde Unity.
- `Assets/ThirdParty`: assets externos que permitan esa ubicación; respetar requisitos de cada plugin.
- `SourceAssets`: originales editables de Blender, texturas y audio, fuera del importador de Unity.
- `Documentation/Design`: copias de referencia del GDD v6 y del inventario de plugins/skills.
- `Documentation/Decisions`: acuerdos posteriores al GDD y detalles todavía pendientes.
- `Builds`: compilaciones locales, ya excluidas por el .gitignore existente.

La estructura no incluye todavía clases de gameplay, escenas nuevas, prefabs ni instancias ScriptableObject. Esos entregables se implementan en las siguientes tareas. Conservar los archivos `.meta` y mover assets desde Unity cuando tengan referencias.

## Próximo trabajo

Crear una escena de prueba en `Assets/_PirateLegends/Scenes/Tests` con suelo, obstáculos simples, luz y cámara; después implementar un personaje provisional con caminar, correr, saltar y cámara en tercera persona.
