# Requisitos y aceptación

## Fase 2 implementada
- Registrar/consultar/habilitar/deshabilitar múltiples carpetas; rechazar duplicados por normalización, solapamientos y raíces con enlaces. Una raíz inexistente puede registrarse y reporta error al analizar.
- Descubrir recursivamente MP3, FLAC, WAV, M4A, AAC, OGG, OPUS y WMA por extensión; omitir otros archivos. Continuar ante errores por archivo/directorio.
- Comparar ruta, tamaño y mtime UTC; omitir lectura/hash en archivos válidos sin cambios. Nuevos/modificados: hash streaming y metadatos.
- Clasificar New, Modified, Unchanged, Moved, Missing, Error; mantener Unavailable para desconexiones/lectura fallida. Ambigüedad explícita, sin fusionar identidades por metadatos o duplicados de contenido conocidos.
- Metadatos: título, artista, artista de álbum, álbum, duración, pista, disco, año, género, bitrate, frecuencia. Datos inexistentes opcionales; si parser falla, conservar datos previos o nombre/ruta y reintentar después.
- Analizar sin escribir biblioteca ni historial; aplicar por confirmación explícita y token de preview. Revalidar revisión de SQLite y filesystem; rechazar obsolescencia. Transacción única para tracks/sesión/resultados/revisión.
- CancellationToken, progreso, trabajo fuera de UI. No borrar registros ausentes, tocar música ni escribir etiquetas.
- Ventana mínima para ruta/selector, habilitación, análisis, progreso, resumen, lista acotada, aplicar y cancelar. Docker ejecuta lógica/tests sin GUI ni audio.

## Aceptación y cobertura
Pruebas con WAV sintético, archivos corruptos y SQLite temporal: primera importación, segundo análisis sin reprocesar, nuevo/modificado/renombrado/movido entre raíces, ID/preferencias, missing/desconexión/recuperación, inaccesibilidad inyectada, enumeración parcial, extensiones, etiquetas, symlink/ciclo, cancelación en análisis/aplicación/batch, persistencia/reapertura, duplicados, preview sin mutación, filesystem/DB obsoletos, apply doble, rollback por fallo y migración v1→v2. Resultados reales en VERIFICATION.md.

No se promete detección de cambios con tamaño/fecha iguales en archivos omitidos, identidad de movimientos de registros sin hash, sensibilidad de rutas por volumen, interrupción instantánea del parser, atomicidad filesystem+DB ni rendimiento de millones de canciones sin benchmark. No se validó GUI/selector/controles en Windows aún.

## Próximas fases
Normalización relacional de artista/álbum, migraciones evolutivas, paginación, búsqueda FTS5 sin perder cola/contexto; reproducción MP3/FLAC/WAV según motor, gapless, ReplayGain, ecualizador, crossfade, playlists, cola y controles Windows. Velocidad persistida por canción (1.25x/1x/0.90x), predeterminada y preservación de tono cuando el motor lo permita.

Cinema: editor de portada/letras/controles/fondos/blur/visualizadores/tipografías/transiciones/layouts/perfiles/múltiples monitores. Extensiones explícitas, dispositivos, automatizaciones e IA solo con alcance autorizado. Migración MusicBee mediante formatos documentados y verificables; sin ingeniería inversa, DRM ni compatibilidad completa prometida.

Objetivo Windows; núcleo probado en Linux; Linux/macOS GUI/audio pendientes por plataforma. App local/offline tras instalar dependencias; Docker solo desarrollo. No servicios web, PostgreSQL ni servidores adicionales.
