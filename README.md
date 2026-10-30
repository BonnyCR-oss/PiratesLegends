# PiratesLegends

Juego 3D para PC de Bonny Crespo. Cooperativo previsto para 2-4 jugadores con Photon Fusion 2.

## Abrir el proyecto

1. Instalar Git y Git LFS y ejecutar `git lfs install`.
2. Clonar el repositorio y ejecutar `git lfs pull`.
3. Abrir desde Unity Hub con la version de `ProjectSettings/ProjectVersion.txt` (actualmente 6000.5.6f1).
4. Unity regenera Library y restaura paquetes usando manifest.json y packages-lock.json.

## Reglas de versionado

- Versionar Assets con sus .meta, Packages y ProjectSettings. Mover assets desde Unity conservando sus GUID.
- No subir Library, Temp, Logs, UserSettings ni builds.
- Binarios de modelos, texturas, audio y video usan Git LFS. Escenas, prefabs, scripts y .meta permanecen en Git normal.
- Para assets .asset excepcionalmente grandes y binarios (por ejemplo TerrainData), agregar una regla LFS por ruta antes de su primer commit. No pasar todos los .asset a LFS.
- No guardar claves API, tokens o secretos en el repositorio. Usar variables de entorno o archivos locales ignorados. Un Photon App ID de cliente no sustituye autenticacion ni debe confundirse con un secreto de servidor.
- Revisar `git diff` y `git status` antes de cada commit. Hacer commits pequenos y guardar la escena antes de confirmar.
- Antes de trabajar: `git pull --ff-only`. Resolver cambios divergentes conscientemente; no usar force-push sobre main.
- Al clonar en otra maquina, configurar UnityYAMLMerge apuntando al Editor instalado. La configuracion local de Git no viaja con el repositorio.
- Git LFS puede tener cuotas de almacenamiento y transferencia en el proveedor remoto.

## Autoria y licencias

Autor del desarrollo propio: Bonny Crespo. Los commits del proyecto se crean con su identidad Git.
Assets de terceros, Unity, Photon y otras dependencias conservan sus respectivas licencias y autoria.
No se concede una licencia publica sobre el desarrollo propio por publicar o almacenar el repositorio.
