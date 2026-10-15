# Variables compartidas. Editables por entorno.
$ErrorActionPreference = 'Stop'
$script:Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:VenvDir = if ($env:NEZZEN_VENV) { $env:NEZZEN_VENV } else { Join-Path $env:LOCALAPPDATA 'NezzenMusicV2\venv' }
$script:Port = if ($env:NEZZEN_PORT) { $env:NEZZEN_PORT } else { '8765' }
$script:VenvPython = Join-Path $VenvDir 'Scripts\python.exe'

function Assert-LocalDrive {
    if ($Repo.StartsWith('\\')) {
        throw "El repositorio está en una ruta UNC ($Repo). Ejecuta primero scripts\windows\sync.ps1 desde WSL y usa la copia local (p. ej. C:\src\nezzen_music_v2)."
    }
}
