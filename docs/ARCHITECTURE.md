# Arquitectura

## Capas
Desktop → Application, Infrastructure, Audio. Infrastructure → Application → Core. Audio → Core. Core solo usa BCL; ningún proyecto central depende de la GUI. Se conserva la estructura modular de fase 1.

- Core: Track, Artist, Album, MusicFolder, ScanSession, ScanResult, FileFingerprint y claves/comparación de rutas.
- Application: MusicFolderService, IncrementalLibraryScanner y contratos de filesystem, metadatos, huellas y persistencia. Coordina análisis/aplicación y protección de preview.
- Infrastructure: SQLite, PhysicalMusicFileSystem, StreamingFileFingerprinter y TagLibMetadataReader.
- Audio: contrato de capacidades y reproducción, sin motor.
- Desktop: composición y ViewModel MVVM; comandos async, selección de carpetas y confirmación de cambios.

## Análisis incremental
1. Obtener snapshot transaccional de biblioteca y revisión global. Leer carpetas registradas; deduplicar IDs solicitados y omitir deshabilitadas. Rechazar solapamientos entre raíces.
2. Descubrir archivos por extensión, directorio por directorio, con cancelación. No seguir enlaces/reparse points. Capturar errores por entrada y por directorio; no abortar por archivo dañado. Una enumeración con errores se considera incompleta de forma conservadora para toda la raíz.
3. Indexar rutas con comparación del SO: case insensitive en Windows, ordinal en Linux/macOS. Usar tamaño/mtime UTC para omitir metadatos y hash de archivos válidos sin cambios. Actualizar LastVerifiedUtc y disponibilidad al aplicar. Si la lectura previa de metadatos falló, reintentar aunque el stat no cambie.
4. Nuevos/modificados: SHA256 streaming (buffer de 64 KiB), metadatos TagLib# de solo lectura y comprobación del stat antes/después. Metadatos inválidos: conservar los anteriores o nombre/ruta si es nuevo; persistir MetadataReadFailed para reintentar. Un fallo de hash/lectura no sustituye los datos anteriores; informa Error y Unavailable si el registro ya existe.
5. Reconciliar ausencias solo en raíces enumeradas completamente. Coincidencia de tamaño y hash entre archivo nuevo y registro ausente, única en ambos conjuntos y sin duplicados conocidos: Moved con confianza ContentHash, ID y preferencias conservados, incluso entre dos raíces seleccionadas. Ambigüedad: New + Missing, con candidato explícito en mensaje y contador PossibleMoves; nunca combinar por título/artista. Archivos de contenido duplicado que siguen existiendo permanecen separados.
6. Emitir resumen y entradas. Un archivo puede tener entrada New/Modified y Error de metadatos: el contador de errores no es una partición exclusiva de los demás contadores. Una raíz inaccesible genera Error y las canciones ausentes observadas quedan Unavailable, nunca se borran.

El hash se calcula una vez al incorporar/modificar para tener evidencia disponible si desaparece la ruta original. No puede reconstruirse después el hash de un registro legado ya ausente. Fase 1 sin hash conserva ID por ruta, pero movimientos no verificados se mantienen New/Missing. Si cambian etiquetas, cambia el hash de archivo: no equivale a huella acústica.

## Vista previa y aplicación
AnalyzeAsync no escribe en SQLite. Conserva una sola PendingScan privada ligada al scanner; devuelve resumen/entradas de solo lectura y token Guid. Un nuevo análisis descarta la anterior. La API ScanAsync antigua también es solo análisis; nunca aplica implícitamente.

ApplyAsync(token) serializa operaciones de la instancia, verifica token/revisión, reenumera raíces y compara conjunto de rutas, tamaños, fechas y completitud. Recalcula SHA256 solo de nuevos/modificados/movidos antes de confirmar. Un cambio detectado produce StaleScanPreviewException y exige analizar de nuevo. La revisión vuelve a comprobarse dentro de una transacción de escritura SQLite para cubrir cambios concurrentes posteriores al primer chequeo. Cambios de preferencias, carpetas o tracks invalidan el preview.

El token no acepta una lista de cambios editable enviada por UI. Aplicación exitosa consume la preview; la revisión evita aplicar dos veces aun desde otras instancias. No hay atomicidad distribuida entre filesystem y SQLite: existe una ventana inevitable tras revalidación; tamaño/fecha iguales ocultan cambios de archivos omitidos. No bloquear todos los archivos de una biblioteca grande ni afirmar protección absoluta contra sustituciones maliciosas de rutas durante el recorrido.

## Persistencia y migración v1→v2
SQLite directo, consultas parametrizadas, foreign keys, conexiones sin pooling. user_version=2; rechazo de esquemas futuros. Inicialización y migración son transaccionales. Se conserva payload JSON del Track, ID, AddedUtc y velocidad; propiedades nuevas tienen defaults compatibles.

- music_folders: id, path, path_key único, enabled.
- tracks: id, folder_id, path, path_key único, content_hash, payload; índices por raíz y hash.
- library_state: revisión global monotónica; incrementa toda escritura de biblioteca.
- scan_sessions: ID, fecha indexada, sesión y resumen JSON.
- scan_entries: sesión + ordinal, resultado por archivo.

CommitScanAsync escribe tracks, sesión, resultados y revisión en una sola transacción. Ante excepción/cancelación revierte todo; el chequeo final de cancelación precede al commit. No hay DELETE de música ni eliminación de registros ausentes. El historial corresponde a aplicaciones confirmadas; preview/cancelación no generan sesiones persistentes. Backups de DB antes de actualizar versiones son responsabilidad del flujo de distribución futuro; probar migraciones en copia. Si existen colisiones de rutas al normalizar un volumen, la migración falla y revierte en lugar de fusionar identidades.

Artist y Album siguen como modelos sin repositorios relacionales propios; sus nombres y demás metadatos se conservan en Track/payload. Normalización relacional, FTS5 y paginación quedan fuera de esta fase.

## Rendimiento y UI
El scanner ejecuta E/S y SQLite fuera del hilo UI mediante Task.Run. TagLib# y Microsoft.Data.Sqlite son esencialmente síncronos; cancelación se comprueba entre archivos y antes/después del parser, no interrumpe instantáneamente una llamada síncrona. SHA256 streaming sí recibe CancellationToken. Transacción reutiliza comandos y evita consultas de biblioteca por archivo. Índices/hash dictionaries aceleran reconciliación, pero snapshot/propuestas ocupan O(n) en memoria: no se promete rendimiento sin medir bibliotecas grandes.

Progreso durante descubrimiento indica raíz; Found se actualiza al terminar de enumerar cada raíz y Processed durante el procesamiento. Notificaciones por archivo limitadas aproximadamente a 10/s, con primera/final inmediatas. La GUI muestra 500 entradas como máximo y resumen global. Registrar/habilitar, analizar y aplicar son acciones separadas; cerrar/cancelar solicita cancelación. GUI nativa no validada en este entorno; Playwright no corresponde a Avalonia nativa.

## Extensiones futuras
FileSystemWatcher invalidará raíces, sin reemplazar reconciliación. Cola y navegación separadas de resultados de búsqueda; acciones ahora/siguiente/añadir/origen/artista/álbum no reemplazarán cola implícitamente. Evaluar motor de audio según formatos, gapless, DSP, velocidad/tono, licencias y empaquetado; aplicar velocidad individual o predeterminada mediante IAudioPlayer. Cinema tendrá layouts versionados, componentes, perfiles y multimonitor; proveedores de letras, visualizadores, metadatos, dispositivos e IA mediante servicios explícitos antes de plugins dinámicos. Migración MusicBee solo con formatos verificados y APIs/exportaciones públicas.

## Validación de rendimiento de fase 2
El harness independiente `tools/Nezzen.Scanner.Benchmarks` consume los servicios reales de Infrastructure/Application sin mocks ni nuevas dependencias y no altera el escáner. El perfil Compose benchmark limita CPU/memoria y no monta datos del host. Se midieron nueve casos hasta 50.000 fixtures de 172 bytes. La etapa Apply domina el análisis sin cambios; antes de optimizarla, perfilar internamente y fijar un objetivo de latencia. Ver BENCHMARKS.md para números/método y WINDOWS_VALIDATION.md para la validación gráfica aún pendiente.
