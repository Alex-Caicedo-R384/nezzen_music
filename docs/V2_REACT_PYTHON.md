# Nezzen Music v2 — React + Python (Entrega A)

Migración en curso desde .NET/Avalonia hacia una aplicación web local. El proyecto C# (`src/`, `tests/`, `tools/`) **no se toca** y su base `library.db` no se lee ni se sobrescribe.

## Hallazgos de la inspección del proyecto .NET
- Esquema SQLite (.NET, v2): `music_folders`, `tracks(payload JSON, path_key, content_hash)`, `library_state(revision)`, `scan_sessions`, `scan_entries`; DB en `%LOCALAPPDATA%\NezzenMusic\library.db`.
- Escáner: `PhysicalMusicFileSystem` + `TagLibMetadataReader` + `StreamingFileFingerprinter` con SHA256 al incorporar/modificar y flujo analizar → aplicar con revisión optimista.
- **Discrepancia:** el repositorio (único commit `f19cd08 prototype`, sin otras ramas) **no contiene** los modos Quick/Standard/Deep ni lógica de placeholders de Google Drive que el encargo describe; no aparecen en `src/`. Se asume que viven en trabajo no incluido en este repo. El escáner Python (Entrega B) se diseñará desde el comportamiento descrito, no desde código. La comparación de rendimiento solo podrá hacerse contra el escáner de fase 2 existente (`tools/Nezzen.Scanner.Benchmarks`).
- Riesgos de migración: metadatos como JSON en `payload` (no ordenable en SQL), hashes completos que leen contenido (peligroso con archivos Drive "solo online"), UI limitada a 500 filas.

## Decisiones
| Decisión | Motivo |
|---|---|
| Base nueva `nezzen_v2.db` en `%LOCALAPPDATA%\NezzenMusicV2` (`~/.local/share/nezzen-music-v2` en Linux) | No tocar `NezzenMusic\library.db`. Configurable con `NEZZEN_DATA_DIR`. |
| Columnas reales en `tracks` (no JSON) | Paginación/orden/filtros en SQL (Entrega B). |
| Alembic aplicado al arrancar | Esquema versionado y reproducible. |
| Backend solo en `127.0.0.1`; `run.py` rechaza otros hosts | Requisito de seguridad. CORS solo `127.0.0.1/localhost:5173`. |
| FastAPI sirve `frontend/dist` si existe | Inicio simple en producción local; en dev, Vite con proxy `/api`. |
| Código en WSL, backend en Windows: `sync.ps1` copia a `C:\src\nezzen_music_v2` | Acceso directo a `G:` sin UNC en ejecución; venv fuera del repo. |
| Tailwind v4 + componentes tipo shadcn escritos a mano sobre Radix | Evitar CLI interactiva; se añaden solo los usados. |
| TanStack Table/Virtual aún no instalados | Se añaden en la Entrega B al usarse. |
| Preferencias visuales en `localStorage` | Provisional; pasarán a SQLite (`app_settings`) en la Entrega B. |

## Estructura
`backend/app/{api,core,db,models,schemas,repositories,services/{scanner,metadata,artwork,audio,library}}`, `backend/alembic`, `backend/tests`; `frontend/src/{app,components,features,hooks,lib,styles,types}`; `scripts/{windows,wsl}`. Los paquetes de servicios están vacíos a propósito hasta sus entregas.

## Funcionalidad real en la Entrega A
- `GET /api/health` (estado, versión, revisión de esquema, conteos, ruta de la DB).
- Migración inicial: `app_settings`, `music_folders`, `tracks`.
- Shell: barra superior con menús, barra lateral con 8 vistas, panel derecho, reproductor inferior, barra lateral redimensionable (ratón y flechas), tema oscuro/claro, 5 acentos.
- **No funcional todavía (deshabilitado o marcado con su entrega):** búsqueda, escaneo, tabla, reproducción, cola, listas, modo cine.

## Entorno
WSL (código, Node, pruebas): `scripts/wsl/test.sh`, `dev-frontend.sh`, `build-frontend.sh`.
Windows (backend con acceso a `G:`; Python 3.12+, sin Node): `sync.ps1` → `setup.ps1` → `start.ps1`; `restart.ps1`, `test.ps1`.
Variables: `NEZZEN_PORT` (8765), `NEZZEN_FRONTEND_PORT` (5173), `NEZZEN_DATA_DIR`, `NEZZEN_VENV`, `NEZZEN_BACKEND_URL`.
