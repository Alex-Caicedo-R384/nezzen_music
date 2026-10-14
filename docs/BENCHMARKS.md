# Benchmark del escáner — fase 2

## Resultados medidos (Linux, no Windows)

Tres repeticiones por tamaño, proceso secuencial, corpus/DB nuevos por repetición; warmup completo de 100 archivos excluido. Medianas de duración/throughput; memoria es el máximo residente muestreado entre Analyze/Apply y las tres repeticiones. Los totales son medianas calculadas sobre la suma por repetición, por lo que pueden diferir de sumar medianas redondeadas.

| Archivos procesados | Escenario | Analizar (s) | Aplicar (s) | Total (s) | Throughput análisis (arch/s) | Throughput total (arch/s) | RSS muestreado máx. (MiB) |
|---:|---|---:|---:|---:|---:|---:|---:|
| 1,000 | Primera importación | 0.108 | 0.084 | 0.189 | 9,239 | 5,291 | 95.1 |
| 1,000 | Sin cambios | 0.031 | 0.080 | 0.113 | 32,055 | 8,872 | 94.8 |
| 1,000 | Cambios controlados | 0.038 | 0.071 | 0.108 | 26,426 | 9,240 | 94.8 |
| 10,000 | Primera importación | 0.533 | 0.450 | 0.983 | 18,755 | 10,170 | 125.6 |
| 10,000 | Sin cambios | 0.100 | 0.334 | 0.428 | 100,246 | 23,356 | 131.0 |
| 10,000 | Cambios controlados | 0.126 | 0.312 | 0.437 | 79,602 | 22,860 | 124.4 |
| 50,000 | Primera importación | 2.936 | 2.274 | 5.210 | 17,032 | 9,598 | 229.6 |
| 50,000 | Sin cambios | 0.527 | 1.587 | 2.114 | 94,892 | 23,652 | 237.5 |
| 50,000 | Cambios controlados | 0.567 | 1.596 | 2.183 | 88,216 | 22,907 | 245.2 |

## Lecturas verificadas

| Tamaño | Metadatos / hashes Analyze / hashes Apply en importación | Sin cambios (los tres contadores) | Cambios (los tres contadores) |
|---:|---|---|---|
| 1,000 | 1,000 / 1,000 / 1,000 | 0 / 0 / 0 | 30 / 30 / 30 |
| 10,000 | 10,000 / 10,000 / 10,000 | 0 / 0 / 0 | 300 / 300 / 300 |
| 50,000 | 50,000 / 50,000 / 50,000 | 0 / 0 / 0 | 1,500 / 1,500 / 1,500 |

Los candidatos a movimiento también leen metadatos antes de reconciliarse. Apply vuelve a leer contenido solo cuando se leyó en Analyze. Los assertions del harness verifican clasificaciones, progreso, contadores de lectura/hash y cardinalidad final. Un error termina con salida distinta de cero.

## Entorno y método

- Medición UTC: `2026-10-14T20:37:45.8041251+00:00`.
- `Ubuntu 24.04.5 LTS`, WSL2 kernel 6.6.87.2-microsoft-standard-WSL2; X64; .NET 10.0.13; SDK 10.0.402.
- CPU host: Intel Core Ultra 9 285H; contenedor limitado a 2 CPU (`cpu.max=200000 100000`) y 2 GiB (`memory.max=2147483648`). Host compartido, sin aislamiento exclusivo.
- Imagen SDK base: `mcr.microsoft.com/dotnet/sdk@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa`.
- Fixtures WAV PCM válidos de **172 bytes**, cada contenido único; 100 archivos por subdirectorio. No MP3/FLAC reales ni música personal. E/S del corpus/DB bajo /tmp del contenedor, sin volúmenes montados ni red durante ejecución.
- Preparación de corpus/SQLite y mutaciones se miden aparte en JSON, fuera de Analyze/Apply. Setup no está incluido en throughput.
- Cambios: 1% nuevos, 1% modificados (mismo tamaño, mtime distinto), 1% movidos, 1% ausentes, sobre grupos distintos. Ejemplo 50000: 500 de cada clase, 48500 sin cambios. Archivos físicos finales=50000; registros finales=50500 porque Missing se conserva.
- Stopwatch; muestreo de WorkingSet/heap administrado cada ~25 ms y en extremos. Incluye baseline del proceso y coste del sampler; no es un pico exacto ni memoria exclusiva de tracks. AllocatedBytes es asignación de todo el proceso. GC explícito antes de cada etapa; no representa pausas normales de una UI.
- JIT calentado; caché de filesystem **no** vaciada. Un proceso comparte cachés/GC a lo largo de repeticiones. Medianas de tres ejecuciones exploratorias, no intervalo de confianza ni garantía de SLA.
- Pico global WorkingSet del proceso: 274.5 MiB; incluye preparación, lecturas finales y otras operaciones fuera de etapas. No confundir con picos muestreados de la tabla.

- 1,000: máximo tamaño lógico final del corpus+DB 2.90 MiB; DB 2.74 MiB (tres sesiones).
- 10,000: máximo tamaño lógico final del corpus+DB 28.42 MiB; DB 26.78 MiB (tres sesiones).
- 50,000: máximo tamaño lógico final del corpus+DB 142.36 MiB; DB 134.16 MiB (tres sesiones).

El espacio físico asignado no se midió: un fixture de 172 bytes puede ocupar un bloque de 4 KiB. Solo se mantiene un corpus por vez y se elimina su raíz propia en finally. Los nueve casos completaron su limpieza; fixtures-only conserva la raíz intencionadamente para pruebas manuales.

## Reproducción

Desde la raíz del repositorio:

```sh
docker compose --profile benchmark build benchmark
docker compose --profile benchmark run -T --rm benchmark --sizes 1000,10000,50000 --repetitions 3 > results.json
# Prueba rápida:
docker compose --profile benchmark run -T --rm benchmark --sizes 1000 --repetitions 1
```

Ejecución nativa (con SDK 10):

```sh
dotnet run --project tools/Nezzen.Scanner.Benchmarks -c Release -- --sizes 1000,10000,50000 --repetitions 3 > results.json
```

Salida JSON en stdout, progreso/errores en stderr. La ejecución nativa no impone los límites Docker: registrar SO, filesystem, SDK y recursos al compararla. El harness no pertenece a Nezzen.sln para no alterar los siete proyectos del producto/tests; Docker lo compila por separado y no añade paquetes nuevos. Las imágenes 10.0 son mutables; se registra el digest usado, y una repetición futura puede fijarlo explícitamente.

## Interpretación y próximos pasos

La comparación incremental evita leer contenido/metadatos en el segundo análisis. A 50000, Analizar sin cambios tarda ~0.53 s y Aplicar ~1.59 s: la aplicación/revalidación/persistencia domina el total. Esto justifica perfilar esa etapa si se exige menos latencia; no identifica por sí solo qué operación interna es el cuello de botella. No se optimizó código de producción.

Antes del motor: completar el checklist Windows, repetir medición nativa en NTFS y medir cancelación/respuesta de UI. Estos fixtures aíslan coste de archivos/metadatos/SQLite y no representan hashes de canciones de varios MB, discos USB lentos, antivirus, red ni decodificación de audio. Un ensayo posterior con corpus sintético acotado de mayor tamaño permitirá separar latencia por archivo de throughput de bytes, sin música personal.

[JSON con las nueve ejecuciones, tiempos y memoria](benchmarks/phase2-linux-docker.json) · [Validación Windows pendiente](WINDOWS_VALIDATION.md)
