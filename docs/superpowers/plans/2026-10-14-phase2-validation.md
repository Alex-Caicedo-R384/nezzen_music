# Validación de fase 2

Objetivo: comprobar acceso/SDK Windows, procedimiento GUI y medir scanner real sin optimizar producción.
Restricciones: develop, sin instalaciones/commits/push, sin música personal, sin motor.

1. Inspeccionar documentación y comprobar SDK Windows desde PowerShell, sin instalar.
2. Añadir harness consola aislado (sin dependencias nuevas) referenciando Infrastructure; fixtures WAV PCM únicos de 172 bytes, raíces temporales, DB propia. Sin aceptar rutas musicales como entradas.
3. Medir Analyze/Apply por separado en importación, sin cambios y cambios de 1% nuevos/1% modificados/1% movidos/1% ausentes (conjuntos separados); tamaño final igual al inicial. Verificar contadores esperados y cero hash/metadatos en segundo análisis. Tres repeticiones por 1000/10000/50000; JSON con tiempos, throughput, memoria muestreada, lecturas/hash y tamaño de DB.
4. Ejecutar Docker sin red ni mounts de música, publicar resultados. Validar suite existente y revisar harness.
5. Documentar comando Windows y checklist GUI aislado con cuenta Windows de prueba; no declarar GUI validada sin interacción.

No medir cache fría: JIT calentado y caché del SO no vaciada. Memoria muestreada ~25ms, no pico exacto. Comparar medianas; fixtures pequeños no modelan throughput de MB de audio real. No optimizar scanner en esta tarea.

## Resultado
- [x] Windows consultado: PowerShell disponible, SDK no detectado; GUI pendiente y procedimiento manual publicado.
- [x] Harness real aislado, revisión independiente sin defectos importantes.
- [x] 9 casos medidos con salida 0; tablas/JSON/digest/SDK/hashes de fuentes publicados.
- [x] 51/51 pruebas existentes aprobadas; Compose válido.
- [x] Documentación actualizada sin optimizar producción ni comenzar motor.
