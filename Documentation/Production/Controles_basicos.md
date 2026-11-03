# Controles básicos del prototipo

Escena: Assets/_PirateLegends/Scenes/Tests/SCN_GameplayTest.unity.

Pulsa Play y haz clic en la vista Game.

- W/A/S/D: caminar relativo a la orientación horizontal de la cámara.
- Shift izquierdo o derecho, mantenido: correr.
- Espacio: saltar cuando hay suelo, con una pequeña tolerancia al borde y pulsación anticipada al aterrizaje.
- Ratón: girar cámara en tercera persona, con límites verticales y detección de paredes.
- Escape: liberar cursor; clic izquierdo: capturarlo nuevamente.

## Componentes

- PlayerInputReader: único lector del Input System; bindings de teclado/ratón y estado de cursor.
- PlayerLocomotion: CharacterController como único responsable del desplazamiento, gravedad, salto y enlace con Animator.
- ThirdPersonOrbitCamera: cámara local orbital; referencias al jugador guardadas en la escena.

Velocidades iniciales: caminar 2,5 m/s, correr 5,5 m/s. Altura de salto 1,25 m. Estos parámetros se ajustan en el Inspector. Se reutilizan el Animator y el salto elegido por el usuario. Los controles todavía son locales, sin sincronización de red ni combate.

## Verificación

Compilación sin errores. Prueba temporal con dispositivos virtuales del Input System: cuatro direcciones respondieron, carrera recorrió 2,57 m frente a 1,35 m caminando en el mismo intervalo, salto alcanzó 1,23 m y aterrizó, ratón giró cámara 19,2 grados. La simulación avanzó 1741 fotogramas. Captura de cámara revisada y consulta final de consola sin errores ni advertencias. Se salió de Play; no se guardaron desplazamientos de la prueba.
