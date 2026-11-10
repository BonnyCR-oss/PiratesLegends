# Efectos de las habilidades de Frostbound

Los controles siguen siendo 1, 2 y mantener/soltar 3. Los efectos se sincronizan con los puntos de preparación, golpe y disparo de los clips existentes.

- **Impacto glacial:** runas y escarcha en las manos, objetivo luminoso, dos ondas expansivas, grietas ramificadas y estacas que brotan en secuencia; fragmentos, vapor frío y golpe grave de cristales.
- **Lanzas:** tres cristales orbitan durante la preparación y cada disparo parte de su posición visible. Estela de energía, nieve fina y explosión de fragmentos al colisionar.
- **Arco:** formación del arco con partículas, runas crecientes en las manos, cuerda luminosa y energía alrededor de la flecha tensada. La liberación genera un pulso, estela más larga y explosión de cristales. Se conserva la pose de tensado y la disolución del arco.

`FrostboundVfx` gestiona la presentación; el daño permanece en FrostboundAbilities/FrostboundProjectile. Los efectos decorativos no tienen colisiones activas. Los sonidos originales se generan de forma determinista con `Tools/Audio/create_frostbound_sfx.py`. Sus seis clips están en Audio y las referencias se guardan en PF_Player_Frostbound.

El shader URP de energía usa color de vértice, bordes suaves y mezcla aditiva. El vapor usa mezcla alfa. No necesita texturas externas ni un volumen de posprocesado. La sacudida se aplica después de la cámara orbital, sin acumular desplazamiento. En el componente FrostboundVfx se pueden ajustar Volume y Camera Impulses.

Se corrigió la orientación del prefab IceSpike: su eje largo estaba horizontal; ahora mide aproximadamente 1,17 m en vertical y su base queda sobre el suelo.

Capturas y comprobaciones del flujo real de teclado en `Captures/Frostbound/`: preparación del impacto, impacto, lanzas orbitando, arco tensado y explosión. El informe `PlayModeReport.json` incluye daño, obstáculos, proyectiles fallidos, limpieza del arco y cancelación durante la preparación de las lanzas.
