# Validación manual de Windows — pendiente

## Estado real
Se pudo ejecutar PowerShell nativo desde WSL. Reportó Windows NT 10.0.26300.0; `Get-Command dotnet` no encontró el ejecutable. No existía dotnet.exe en Program Files, Program Files (x86), LocalAppData/Microsoft/dotnet ni UserProfile/.dotnet. No se instalaron herramientas. No se ejecutó la ventana ni se interactuó con el selector: **GUI Windows no validada**. Esto no descarta instalaciones en rutas no estándar que no estén en PATH.

## Preparación
Usar una cuenta Windows de prueba separada para que `%LOCALAPPDATA%\NezzenMusic` no contenga biblioteca personal. No registrar carpetas personales. Si no se dispone de cuenta/entorno aislado, posponer la prueba; no renombrar ni borrar automáticamente bases de datos existentes.

Una vez disponible un SDK .NET 10 estable, comprobar en PowerShell:

```powershell
dotnet --info
dotnet --list-sdks
```

No instalar SDK ni dependencias automáticamente. Si sigue sin SDK, la validación permanece bloqueada hasta que el usuario prepare el entorno.

Trabajar sobre una copia Windows de los **archivos actuales** de develop, porque todavía no hay commits: un clone del remoto no garantiza estos cambios. Copiar fuentes/configuración a una carpeta de prueba como `C:\src\nezzen_music-validation`, excluyendo `.git`, `.superpowers`, `bin`, `obj` y cualquier DB. No compartir bin/obj entre SDK Windows y WSL. No cambiar la rama del repositorio original.

```powershell
Set-Location C:\src\nezzen_music-validation
dotnet restore Nezzen.sln
dotnet build Nezzen.sln -c Release --no-restore
dotnet test Nezzen.sln -c Release --no-build
# Genera solo WAV mínimos en un directorio temporal nuevo. No analiza música existente.
dotnet run --project .\tools\Nezzen.Scanner.Benchmarks\Nezzen.Scanner.Benchmarks.csproj -c Release -- --fixtures-only 1000
# Copiar Root y Music del JSON; conservarlos para la prueba.
dotnet run --project .\src\Nezzen.Desktop\Nezzen.Desktop.csproj -c Release --no-build
```

El último comando es la ejecución **nativa en Windows** correcta desde la raíz de la copia. No usar el contenedor para validar la GUI. Los fixtures retenidos no se borran automáticamente en modo fixtures-only; borrar después solo el directorio Root generado que contiene `.nezzen-synthetic-only`, nunca una ruta elegida arbitrariamente.

## Comprobación reproducible
Anotar SDK, SO, arquitectura, DPI/escalado, ruta de copia y resultados observados. Todos los pasos siguientes están **pendientes**, no aprobados por esta sesión.

| Paso | Acción | Resultado esperado |
|---|---|---|
| Apertura | Ejecutar Desktop con el comando anterior | Ventana visible, sin error; controles utilizables con teclado, sin recortes a escalado habitual |
| Selector | Pulsar Seleccionar, navegar a Music del JSON; también cancelar el diálogo | Se rellena la ruta al elegir; cancelar conserva el estado sin errores |
| Registro | Registrar Music, registrar otra vez y alternar habilitar/deshabilitar | Una raíz, sin duplicados; estado cambia; deshabilitada no se analiza |
| Análisis | Habilitar y Analizar habilitadas | UI responde; encontrados/procesados llegan a 1000; 1000 nuevos y 0 errores |
| Preview sin aplicación | Cerrar sin aplicar, volver a abrir y analizar | Carpeta persistida; siguen siendo 1000 nuevos: preview no importó tracks |
| Confirmación | Pulsar Aplicar cambios una vez | Finaliza; botón deja de permitir reaplicar esa preview; no cambia ningún WAV |
| Persistencia | Cerrar, reabrir y analizar la misma raíz | 1000 sin cambios, 0 nuevos; datos sobreviven al reinicio |
| Cambio controlado | Renombrar únicamente un fixture dentro de Music y volver a analizar | 1 movido, 999 sin cambios, sin Missing; aplicar conserva la identidad según backend probado |
| Preview obsoleta | Analizar; agregar/copiar un fixture dentro de Music antes de aplicar | Aplicar rechaza la propuesta y pide volver a analizar; no guarda parcialmente |
| Desconexión simulada | Con app inactiva, renombrar solo Music temporal a music-offline; analizar la raíz original | Error/no disponible, sin eliminación ni Missing masivo; restaurar el nombre y analizar de nuevo |
| Cancelación | Generar otro corpus temporal de 10000 o 50000, registrar solo esa raíz habilitada; analizar y cancelar mientras esté en curso | Cancelado, sin importación parcial; análisis posterior empieza desde DB anterior |
| UI de gran volumen | Analizar el corpus grande | Ventana responde; resumen completo; detalle limitado explícitamente a 500 entradas |

Las operaciones pequeñas pueden terminar antes de pulsar Cancelar. En ese caso anotar **no observado**, no darlo por aprobado: repetir con el corpus mayor sin generar audio grande. Cancelar después de que Aplicar terminó no puede deshacer una transacción ya confirmada.

Para comprobar bytes, tomar hashes antes/después de **Analizar/Aplicar** sin introducir mutaciones manuales entre ambas mediciones. Los renombres/copias del checklist se limitan a fixtures sintéticos. Guardar capturas y cualquier excepción literal sin credenciales.

## Registro de resultado
Completar fecha, entorno, cada paso aprobado/fallido/no observado, duración aproximada y evidencia. Sin ese registro, Windows seguirá pendiente. No comenzar el motor de audio únicamente porque el build Linux o el benchmark hayan pasado.
