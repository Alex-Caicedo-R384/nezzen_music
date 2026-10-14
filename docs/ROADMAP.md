# Roadmap

1. **Fundamentos implementados:** solución, entidades, contratos, SQLite básico, Avalonia mínima, xUnit y Docker.
2. **Escáner incremental implementado:** gestión de raíces, TagLib#, hashes streaming, comparación, movimientos conservadores, preview, revalidación, transacción/historial, progreso/cancelación y UI mínima. Ver resultados/limitaciones en VERIFICATION.md. Antes de uso amplio: validar manualmente GUI en Windows y medir bibliotecas grandes.
3. **Biblioteca/búsqueda:** normalización artista/álbum, paginación de snapshot/preview/historial, FTS5 y navegación sin perder contexto/cola; verificación profunda opcional y carga de hashes de registros legados; política por volumen y mejoras de notificación de descubrimiento.
4. **Reproducción:** evaluar motores/licencias/distribución, formatos, cola, velocidad individual/tono y controles multimedia; comprobar por plataforma.
5. **Audio avanzado:** gapless, ReplayGain, DSP/ecualizador y crossfade según capacidades y mediciones.
6. **Cinema:** layouts/perfiles versionados, renderizador/editor, letras/visualizadores y multimonitor.
7. **Integraciones:** migración documentada/verificada, eventos FileSystemWatcher, extensiones y plugins/IA solo con justificación.

GitHub Actions y empaquetado reproducible se añadirán con autorización independiente. No iniciar fase 3 automáticamente, ni commits/push sin autorización.
