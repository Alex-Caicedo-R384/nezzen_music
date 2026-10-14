# Desarrollo

## Entorno
Instalar SDK .NET 10 estable por cuenta del desarrollador; global.json acepta feature bands estables 10.0. No instalar herramientas globales desde tareas automatizadas sin autorización. Windows nativo recomendado para probar UI/audio; WSL2 sirve para lógica y SQLite. No compartir bin/obj entre ejecuciones de SDK Windows y Linux. Linux requiere dependencias gráficas nativas de Avalonia y DISPLAY/Wayland; no asumir audio en WSL.

```sh
git branch --show-current
dotnet --info
dotnet restore Nezzen.sln
dotnet build Nezzen.sln -c Release
dotnet test Nezzen.sln -c Release
dotnet run --project src/Nezzen.Desktop
```

Usar develop. La ventana crea el directorio LocalApplicationData/NezzenMusic e inicializa library.db fuera del hilo UI. Nunca ubicar DB en carpetas musicales. Al instanciar el repositorio manualmente, el directorio padre debe existir y se debe ejecutar InitializeAsync antes de usarlo. Antes de actualizar una instalación existente, respaldar library.db con la aplicación cerrada. Migración v1→v2 es transaccional; si falla, conservar el original y diagnosticar sobre copia.

## Docker
```sh
docker --version
docker compose version
docker info
docker compose config
docker compose run --rm --build tests
docker compose run --rm tests
```
Imagen oficial SDK .NET 10; restaura y compila solución incluyendo Desktop, pero solo ejecuta pruebas de dominio/infraestructura. La ejecución de tests no tiene red ni volúmenes del host. Restore/build requieren Internet inicialmente; app y SQLite funcionan localmente. .dockerignore permite solo fuentes/configuración de compilación; no música, .git ni documentos personales. No GUI, dispositivos de audio, servidores ni PostgreSQL.

## Pruebas y límites
xUnit crea datos ficticios y DB en directorio temporal aislado. No leer música personal. Pruebas comprueban validación, identidad, comparación incremental básica, ausencia/desconexión, persistencia de estado/preferencias, reapertura y cancelación inicial. Fase 2 añade ScannerTests y ScanTransactionTests con WAV sintético, parser real, inyección de inaccesibilidad y fallos controlados de DB. No se prueban audio ni FTS5: no están implementados.

Ver [VERIFICATION.md](VERIFICATION.md) para resultados reales de esta sesión. La compilación Linux no sustituye validación de ventana en Windows ni empaquetado; sin SDK local los comandos se ejecutan dentro de Docker. Microsoft.Data.Sqlite ofrece API async pero SQLite realiza E/S síncrona; mantener esas operaciones fuera del hilo UI al integrar casos de uso.

## Validación de fase 2
Consultar [Windows](WINDOWS_VALIDATION.md) para ejecutar Desktop nativamente con una cuenta/corpus de prueba y [benchmark](BENCHMARKS.md) para repetir mediciones. El harness está fuera de la solución del producto; compilarlo con `dotnet build tools/Nezzen.Scanner.Benchmarks -c Release` o el perfil Compose benchmark. No usar fixtures-only sobre carpetas personales: el comando genera su propia raíz temporal y no acepta rutas de entrada.
