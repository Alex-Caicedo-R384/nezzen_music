# Nezzen Music

Reproductor de escritorio local, modular y original, orientado a Windows y con validación del núcleo en Linux. Inspirado en necesidades funcionales de MusicBee, sin reutilizar código ni recursos propietarios.

## Estado: fase 2
Escáner incremental real con registro/habilitación de múltiples carpetas, búsqueda recursiva de audio, lectura de metadatos, comparación rápida, hashes selectivos, movimientos verificados, vista previa y aplicación transaccional en SQLite. La interfaz mínima permite analizar, revisar y confirmar cambios; no hay motor de reproducción ni búsqueda FTS todavía.

> **Nueva versión web local (React + Python) en desarrollo:** ver [docs/V2_REACT_PYTHON.md](docs/V2_REACT_PYTHON.md). La app .NET permanece intacta.

## Requisitos y comandos
SDK .NET 10 estable para desarrollo nativo. Docker/Compose son opcionales; la aplicación instalada no los necesita.

```sh
dotnet restore Nezzen.sln
dotnet build Nezzen.sln -c Release
dotnet test Nezzen.sln -c Release
dotnet run --project src/Nezzen.Desktop
# Alternativa sin SDK local:
docker compose run --rm --build tests
# Repetir con imagen existente:
docker compose run --rm tests
```

## Uso de la biblioteca
Selecciona o escribe una carpeta y pulsa **Registrar carpeta**. Registra otras raíces separadas si las necesitas; las raíces anidadas se rechazan. Selecciona una raíz para habilitarla/deshabilitarla. **Analizar habilitadas** genera una vista previa sin modificar la biblioteca. Revisa el resumen y pulsa **Aplicar cambios** para confirmar; si cambió la biblioteca o el sistema de archivos, vuelve a analizar. **Cancelar** interrumpe la operación sin guardar lotes parciales.

Se reconocen MP3, FLAC, WAV, M4A, AAC, OGG, OPUS y WMA por extensión; extensión no garantiza audio válido. Los archivos dañados conservan nombre/ruta y un error, con reintento de metadatos en el siguiente análisis. No se modifican archivos ni etiquetas. Ausencias se conservan como Missing; una raíz desconectada o enumeración parcial nunca provoca borrado ni Missing masivo. Un movimiento conserva ID/preferencias solo con hash coincidente y correspondencia única; las coincidencias ambiguas no fusionan registros.

SQLite se crea bajo `Environment.SpecialFolder.LocalApplicationData/NezzenMusic/library.db` (en Windows, normalmente `%LOCALAPPDATA%\NezzenMusic\library.db`). Guarda biblioteca, revisión e historial de análisis aplicados. Las previews viven en memoria, una por instancia, y no sobreviven al cierre. La lista visual muestra los primeros 500 resultados; el resumen y la persistencia incluyen todos.

Windows es el objetivo inicial; UI/selector requieren validación manual nativa. En Linux se necesita sesión gráfica y dependencias de Avalonia; no ejecutar GUI en Docker.

## Tecnologías
C# / .NET 10 LTS; Avalonia 12.1.4; CommunityToolkit.Mvvm 8.4.2; Microsoft.Data.Sqlite 10.0.13; TagLibSharp 2.3.0; xUnit 2.9.3, runner 3.1.4 y Test SDK 17.14.1. Sin EF Core, servidor SQL ni motor de audio. TagLib# se usa con streams de solo lectura, sin `Save()` en producción; consultar su [fuente y licencia LGPL-2.1](https://github.com/mono/taglib-sharp) antes de empaquetar/distribuir el producto. La librería llega como DLL NuGet separada; esta fase no define un instalador ni una licencia global del proyecto.

## Límites conocidos
Tamaño y mtime iguales pueden ocultar modificaciones; no hay modo de verificación profunda general todavía. Se calcula SHA256 al incorporar/modificar para disponer de referencia futura, y se revalida el hash de los cambios al aplicar; no se calcula en archivos válidos sin cambios. Registros de fase 1 sin hash no permiten conservar ID al renombrarlos hasta que tengan una huella fiable. No se siguen enlaces simbólicos/junctions. La política por SO no detecta la sensibilidad de cada volumen. La propuesta usa memoria O(n); se midieron hasta 50.000 fixtures mínimos en Docker, no una biblioteca de millones de canciones. No existe atomicidad conjunta entre SQLite y un sistema de archivos externo que cambie después de revalidarlo.

[Arquitectura](docs/ARCHITECTURE.md) · [Requisitos](docs/REQUIREMENTS.md) · [Desarrollo](docs/DEVELOPMENT.md) · [Roadmap](docs/ROADMAP.md) · [Verificación](docs/VERIFICATION.md)

## Validación y rendimiento
La GUI Windows sigue pendiente: se consultó PowerShell nativo, pero no se detectó SDK .NET en PATH ni ubicaciones habituales. Ver [procedimiento Windows](docs/WINDOWS_VALIDATION.md). Con SDK disponible, desde la raíz de una copia Windows:

```powershell
dotnet run --project .\src\Nezzen.Desktop\Nezzen.Desktop.csproj -c Release
```

Benchmark reproducible de 1.000/10.000/50.000 archivos sintéticos mínimos, sin música personal:

```sh
docker compose --profile benchmark build benchmark
docker compose --profile benchmark run -T --rm benchmark --sizes 1000,10000,50000 --repetitions 3 > results.json
```

Medianas a 50.000 en Linux Docker (2 CPU/2 GiB): importar+aplicar 5,21 s; sin cambios 2,11 s; cambios controlados 2,18 s. No son mediciones de Windows ni archivos de varios MB. [Método, memoria y resultados completos](docs/BENCHMARKS.md).
