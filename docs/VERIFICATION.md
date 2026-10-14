# Validación de fase 2 — Windows y rendimiento — 14 de octubre de 2026

## Estado
**Benchmark Linux completado; GUI Windows pendiente.** Se trabajó en develop, sin commits/push, instalaciones ni cambios al escáner/motor de audio. No se tocó música personal.

## Windows: comprobaciones reales
PowerShell nativo respondió (`powershell.exe -NoProfile -NonInteractive`): `Microsoft Windows NT 10.0.26300.0`. `Get-Command dotnet -ErrorAction SilentlyContinue` no encontró dotnet. `Test-Path` no encontró dotnet.exe en Program Files, Program Files (x86), LocalAppData/Microsoft/dotnet ni UserProfile/.dotnet. WSL tampoco tiene SDK local. No se excluye una instalación no estándar fuera de PATH.

No se lanzó Desktop, no se abrió selector ni se interactuó con la ventana. No se declara Windows validado. El comando correcto en PowerShell, desde la raíz de una copia Windows con SDK 10, es:

```powershell
dotnet run --project .\src\Nezzen.Desktop\Nezzen.Desktop.csproj -c Release
```

[Procedimiento manual reproducible](WINDOWS_VALIDATION.md): cuenta de prueba, corpus temporal, apertura, selector, registro/habilitación, preview sin escritura, confirmar, cancelar, persistencia, obsolescencia y desconexión. Todos los pasos gráficos siguen pendientes de ejecución.

## Benchmark ejecutado
Se añadió un harness consola sin paquetes nuevos, un Dockerfile y perfil Compose benchmark. Usa los servicios reales, WAV sintéticos únicos de 172 bytes, SQLite y directorios temporales propios. No acepta rutas de música como entradas. Generación/mutación fuera de tiempos del scanner, limpieza de la raíz creada en finally; modo fixtures-only la conserva explícitamente para pruebas manuales.

Comandos ejecutados:

```sh
docker compose --profile benchmark build benchmark
docker compose --profile benchmark run -T --rm benchmark --sizes 1000,10000,50000 --repetitions 3 > docs/benchmarks/phase2-linux-docker.json
docker compose run --rm --build tests
docker compose config --quiet
```

- Build del harness/publish Release: correcto. Su ejecución completa terminó con salida 0, nueve casos tras warmup de 100 archivos.
- Smoke inicial: salida 1 porque el harness esperaba cero metadatos para movidos. El scanner lee candidatos antes de reconciliar; se corrigió la expectativa del medidor, sin optimizar/cambiar producción. Se repitió el harness completo con éxito.
- Casos por tamaño: primera importación, sin cambios, cambios de 1% nuevos/1% modificados/1% movidos/1% ausentes. Assertions verifican clasificaciones, progreso, lecturas/hash y cardinalidad final.
- Segunda pasada: cero lecturas de metadatos y cero hashes, en Analyze y Apply, para los tres tamaños y repeticiones.
- Medianas Analyze+Apply (s):

| Archivos | Importación | Sin cambios | Cambios controlados |
|---:|---:|---:|---:|
| 1.000 | 0,189 | 0,113 | 0,108 |
| 10.000 | 0,983 | 0,428 | 0,437 |
| 50.000 | 5,210 | 2,114 | 2,183 |

- Mayor RSS muestreado en etapas: 245,2 MiB; pico global del proceso 274,5 MiB (incluye operaciones fuera de etapas). Muestreo ~25ms, no pico exacto. Máximo tamaño lógico corpus+DB 142,36 MiB; DB 134,16 MiB con tres sesiones. Espacio físico asignado no medido; los fixtures pueden ocupar bloques de 4 KiB. Un solo corpus vivo a la vez, eliminado al terminar cada caso.
- Entorno: Ubuntu 24.04.5 LTS en Docker sobre WSL2, x64, SDK 10.0.402/runtime .NET 10.0.13, host Core Ultra 9 285H, contenedor limitado a 2 CPU y 2 GiB; sin volúmenes del host ni red en runtime. No es Windows nativo ni mide audio grande/caché fría.
- Revisión independiente del harness: sin defectos importantes; observación menor sobre imagen SDK mutable atendida registrando versión/digest concreto y hashes de fuentes. No se introdujeron optimizaciones.

Ver [tablas completas, throughput, memoria y metodología](BENCHMARKS.md), [JSON original](benchmarks/phase2-linux-docker.json) y [SHA256 de fuentes medidas](benchmarks/phase2-source-sha256.txt).

## Regresión del producto
Compose ejecutó nuevamente las **51 pruebas existentes**: Core 14 e Infrastructure 37; todas aprobadas, 0 fallos/omitidas. La imagen de tests fue reutilizada desde caché de build del producto, cuyo código no cambió; no se afirma una compilación nativa de Windows. El harness se compiló separadamente y su ejecución incorporó assertions funcionales. Configuración Compose válida. No se añadieron dependencias a la solución ni se alteraron los siete proyectos originales.

## Recomendaciones antes del motor
Completar primero el checklist GUI Windows y repetir benchmark nativo sobre NTFS con los mismos fixtures. Evaluar respuesta/cancelación de UI y luego un corpus sintético acotado con archivos mayores. A 50.000 sin cambios, Apply (~1,59 s) domina Analyze (~0,53 s): si el objetivo necesita menos latencia, perfilar revalidación/persistencia antes de optimizar. Estas mediciones no justifican atribuir el coste a una operación interna concreta ni prometer tiempos para música real.

Archivos añadidos: tools/Nezzen.Scanner.Benchmarks/{csproj,Program.cs}, docker/Dockerfile.benchmarks, docs/BENCHMARKS.md, docs/WINDOWS_VALIDATION.md, JSON/SHA256 bajo docs/benchmarks y plan de validación. Modificados: compose.yaml, .dockerignore, README, ARCHITECTURE, DEVELOPMENT y este informe. No motor de audio ni siguiente fase.

---

# Verificación activa — Fase 2 — 14 de octubre de 2026

## Resultado final
`docker compose run --rm --build tests` terminó con salida **0**. SDK .NET 10 en contenedor Linux, misma solución de siete proyectos.

- Restore: correcto (dependencias explícitas y auditoría habilitada).
- `dotnet build Nezzen.sln -c Release --no-restore`: **0 advertencias, 0 errores**.
- `dotnet test Nezzen.sln -c Release --no-build --no-restore --nologo`: **51/51 aprobadas**, 0 fallidas, 0 omitidas: Core 14, Infrastructure 37.
- `docker compose config --quiet`: válido.
- Comprobación de referencias con Python: 7 proyectos, referencias existentes, sin ciclos, Core independiente. Comprobación de espacios finales en fuentes/documentación: correcta.
- `git diff --check`: sin errores; archivos siguen untracked porque no hay commits. Se revisaron diferencias contra el respaldo local de fase 1 mediante `git diff --no-index --stat`, además de revisión de código independiente.
- `dotnet --info` en host: salida 127, command not found. No se ejecutaron build/test nativos; se ejecutaron realmente en Docker. No se instalaron herramientas globales ni workloads.

## Inspección y comandos de trabajo
Se inspeccionaron AGENTS.md, arquitectura, contratos, entidades, repositorio SQLite, Desktop, proyectos y pruebas; `git status --short --branch`, `docker info` y búsqueda de instrucciones anidadas. Rama develop sin commits, sin cambio de rama ni modificaciones remotas. Fuentes/configuración existentes respaldadas localmente en `.superpowers/backups/phase2/` (ignorado); sin música personal.

Se verificó la disponibilidad de TagLibSharp 2.3.0 mediante NuGet v3. Context7 resolvió documentación de Avalonia y se consultó StorageProvider/folder picker; no encontró el paquete original TagLib#, por lo que se consultó su repositorio público oficial, sin usar código propietario.

`dotnet format whitespace --folder --verbosity quiet` mediante SDK Docker: salida 0; aviso de verificación de workloads del SDK, sin instalar/actualizar workloads. Dockerfile/Compose existentes se reutilizaron sin introducir servicios ni volúmenes de música.

## Pruebas y correcciones observadas
- RED inicial: las pruebas nuevas no compilaban porque ScanPreview/IncrementalLibraryScanner/IMusicFileSystem aún no existían.
- Primera implementación: build correcto y **38/38** aprobadas.
- Compilación de UI mínima: falló por API obsoleta TextBox.Watermark en Avalonia 12 (CS0618 tratado como error). Corregida a PlaceholderText, sin suprimir advertencias.
- Pruebas ampliadas detectaron cuatro fallos reales: movimiento ambiguo con duplicados conocidos, metadatos anteriores borrados tras error, excepción ArgumentOutOfRange de TagLib# en OGG dañado, y resumen sin Unavailable tras fallo de lectura. Se observó cada fallo antes de corregir; suite posterior **49/49** aprobadas.
- Revisión independiente: confirmó riesgos de pérdida de metadatos y disponibilidad no reflejada; corregidos con estado MetadataReadFailed/reintento y entrada Unavailable. Progreso por archivo acotado aproximadamente a 10/s; límite de legado sin hash documentado.
- Dos regresiones adicionales fallaron antes de corregir: reconexión con archivo aún ilegible y excepción de acceso durante descubrimiento. Se validan archivos previamente no disponibles y se convierten errores de raíz en observaciones incompletas. Resultado final **51/51**.

## Cobertura real
WAV PCM sintético con y sin etiquetas; los tests pueden escribir etiquetas únicamente en sus propios fixtures temporales. Producción usa streams TagLib# de solo lectura. Se verifica que analizar/aplicar no altera bytes de los fixtures.

Casos: primera importación, preview sin escritura/historial, segundo análisis sin lectura/hash de archivos válidos, nuevo/modificado, renombre/movimiento interno y entre raíces, ID/velocidad, missing conservado, desconexión/recuperación, inaccesibilidad inyectada, corrupción y fallback, ocho extensiones/otras ignoradas, metadatos y reintento, duplicados/ambigüedad, rutas duplicadas/anidadas, symlinks/ciclos, cancelación de análisis/aplicación/batch, historial/reapertura, rollback ante fallo SQLite, revisión obsoleta, cambio de filesystem/contenido antes de aplicar, aplicación doble, y migración v1→v2 preservando datos.

La inaccesibilidad se inyecta mediante contratos para que los tests sean deterministas incluso con usuario root en Docker; no se afirma haber validado permisos NTFS o dispositivos desconectados físicamente. La desconexión se simula renombrando únicamente la raíz temporal de la prueba. No se ejecutaron pruebas auditivas ni decodificación completa de ocho formatos: se prueban WAV válido y manejo de extensiones/archivos inválidos; no hay motor de audio.

## Límites pendientes
UI/selector/ventana compilan, pero no se ejecutaron gráficamente ni se probaron en Windows/macOS. No corresponde usar Playwright para esta UI nativa. No hubo benchmark masivo ni validación de sensibilidad por volumen. Snapshot/propuestas/historial usan memoria O(n); UI limita detalle a 500 filas. Cancelación de TagLib#/SQLite síncronos es cooperativa entre operaciones. La comparación rápida no detecta cambios que preserven tamaño/mtime; registros legados sin hash no permiten verificar movimientos. No se garantiza atomicidad entre filesystem y SQLite después de revalidar; enlaces no se siguen, pero no se ofrece sandbox frente a sustituciones maliciosas concurrentes.

Se conserva música y registros ausentes; no hay escrituras de etiquetas en producción. No commits, push, merges, despliegues ni fase 3.

## Archivos de fase 2
Nuevos:
- `src/Nezzen.Core/LibraryPath.cs`
- `src/Nezzen.Application/ScanPreview.cs`
- `src/Nezzen.Application/MusicFolderService.cs`
- `src/Nezzen.Application/IncrementalLibraryScanner.cs`
- `src/Nezzen.Infrastructure/PhysicalMusicFileSystem.cs`
- `src/Nezzen.Infrastructure/StreamingFileFingerprinter.cs`
- `src/Nezzen.Infrastructure/TagLibMetadataReader.cs`
- `tests/Nezzen.Infrastructure.Tests/ScannerTests.cs`
- `tests/Nezzen.Infrastructure.Tests/ScanTransactionTests.cs`
- `docs/superpowers/plans/2026-10-14-phase2.md`

Modificados: MusicFolder, Track, ScanResult; contratos ILibraryRepository/ILibraryScanner/IFileMetadataReader; SqliteLibraryRepository y csproj de Infrastructure (única dependencia añadida: TagLibSharp 2.3.0); App/MainWindowViewModel; README, AGENTS y documentación ARCHITECTURE/REQUIREMENTS/ROADMAP/DEVELOPMENT/VERIFICATION. Sin reestructurar solución, cambiar el motor Audio ni añadir servidores.

---

# Verificación — 14 de octubre de 2026

## Entorno y Git
WSL2 Linux x86_64. Rama develop sin commits, remoto origin inspeccionado sin cambiarlo. SDK local no disponible (`dotnet --info`: command not found). Docker 29.8.2, Compose v5.5.1 y daemon disponibles. Imagen oficial mcr.microsoft.com/dotnet/sdk:10.0 ejecutó SDK 10.0.402.

## Comandos ejecutados y resultados
- pwd, ls -la, rg --files, git status --short --branch, git branch --show-current y git remote -v: repositorio inicialmente vacío, rama develop.
- uname -a, dotnet --info, docker --version, docker compose version y docker info: WSL2; sin SDK local; Docker funcional.
- docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet --version: 10.0.402.
- Consulta a NuGet v3: disponibilidad de paquetes estables; versiones concretas en csproj/README.
- Primera ejecución de dotnet test Core con SDK Docker: falló por tipo FileFingerprint aún ausente, antes de implementar el dominio.
- Primera docker compose run --rm --build tests: restore rechazó NU1903 por dependencias transitivas vulnerables de Avalonia 11.3.8 y Microsoft.Data.Sqlite 10.0.0. Se actualizaron paquetes, sin desactivar auditoría.
- Segunda Compose: build correcto, 0 advertencias/errores, 14/14 pruebas originales aprobadas.
- Regresión de validación init de Track: dotnet test Core dentro de imagen con archivo de prueba montado de solo lectura; 2 fallos esperados (no se lanzó excepción), 12 aprobadas. Se corrigieron Title y Fingerprint.
- dotnet test Nezzen.sln -c Release --no-restore --nologo dentro de imagen con archivos corregidos montados de solo lectura: 16/16 aprobadas.
- Verificación final: docker compose run --rm --build tests, salida 0. Ejecutó dotnet restore Nezzen.sln; dotnet build Nezzen.sln -c Release --no-restore; dotnet test Nezzen.sln -c Release --no-build --no-restore --nologo. Build: 0 advertencias, 0 errores. Core: 14 aprobadas; Infrastructure: 2 aprobadas; 0 fallos/omitidas.
- docker compose config --quiet: válido. Script Python: 7 proyectos, referencias existentes, sin ciclos, Core independiente y sin espacios finales en fuentes. git diff --check sin errores (los archivos nuevos son untracked; el script comprobó también estos).
- dotnet format whitespace --folder dentro del SDK Docker: salida 0. Emitió aviso de comprobación de workloads; no se instalaron/actualizaron workloads ni herramientas globales.

NuGet sufrió descargas parciales y un timeout; sus reintentos completaron la restauración final. No se suprimieron advertencias de vulnerabilidades.

## Revisión y límites
Revisión independiente de archivos nuevos: detectó validación eludible mediante with en Track; corregida con dos pruebas RED→GREEN. Sin otros hallazgos importantes reportados.

No se ejecutó la ventana gráfica ni se validó Windows/macOS/audio. No hay SDK nativo: build/test se ejecutaron en Linux mediante Docker. No se ejecutaron pruebas de escáner completo, FTS5, etiquetas o reproducción porque no están implementados. SQLite mantiene metadata adicional en payload JSON: relaciones relacionales completas y paginación quedan documentadas como trabajo futuro. La política de comparación de rutas por volumen debe resolverse antes del escáner Windows.

No se modificó música real, no se instalaron herramientas globales ni se hicieron commits, push, merges o cambios remotos. El último git log confirmó que develop sigue sin commits.

## Archivos creados

- `.dockerignore`
- `.gitignore`
- `AGENTS.md`
- `Directory.Build.props`
- `Nezzen.sln`
- `README.md`
- `compose.yaml`
- `docker/Dockerfile.tests`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/REQUIREMENTS.md`
- `docs/ROADMAP.md`
- `docs/superpowers/plans/2026-10-14-phase1.md`
- `global.json`
- `src/Nezzen.Application/IFileMetadataReader.cs`
- `src/Nezzen.Application/ILibraryRepository.cs`
- `src/Nezzen.Application/ILibraryScanner.cs`
- `src/Nezzen.Application/Nezzen.Application.csproj`
- `src/Nezzen.Audio/IAudioPlayer.cs`
- `src/Nezzen.Audio/Nezzen.Audio.csproj`
- `src/Nezzen.Core/Album.cs`
- `src/Nezzen.Core/Artist.cs`
- `src/Nezzen.Core/FileChangeClassifier.cs`
- `src/Nezzen.Core/FileFingerprint.cs`
- `src/Nezzen.Core/MusicFolder.cs`
- `src/Nezzen.Core/Nezzen.Core.csproj`
- `src/Nezzen.Core/ScanResult.cs`
- `src/Nezzen.Core/ScanSession.cs`
- `src/Nezzen.Core/Track.cs`
- `src/Nezzen.Desktop/App.cs`
- `src/Nezzen.Desktop/MainWindowViewModel.cs`
- `src/Nezzen.Desktop/Nezzen.Desktop.csproj`
- `src/Nezzen.Desktop/Program.cs`
- `src/Nezzen.Infrastructure/Nezzen.Infrastructure.csproj`
- `src/Nezzen.Infrastructure/SqliteLibraryRepository.cs`
- `tests/Nezzen.Core.Tests/DomainTests.cs`
- `tests/Nezzen.Core.Tests/Nezzen.Core.Tests.csproj`
- `tests/Nezzen.Infrastructure.Tests/Nezzen.Infrastructure.Tests.csproj`
- `tests/Nezzen.Infrastructure.Tests/SqliteTests.cs`
- `docs/VERIFICATION.md`
