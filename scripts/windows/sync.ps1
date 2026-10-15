# Copia backend, scripts y frontend\dist desde WSL a una carpeta local de Windows (sin UNC en tiempo de ejecución).
# Uso: powershell -File \\wsl.localhost\Ubuntu\home\alex\nezzen_music\scripts\windows\sync.ps1 [-Dest C:\src\nezzen_music_v2]
param([string]$Dest = 'C:\src\nezzen_music_v2')
$ErrorActionPreference = 'Stop'
$src = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
New-Item -ItemType Directory -Force $Dest | Out-Null
$xd = @('.venv', '__pycache__', '.pytest_cache', 'node_modules')
robocopy (Join-Path $src 'backend') (Join-Path $Dest 'backend') /MIR /XD $xd /XF *.db *.db-wal *.db-shm /NFL /NDL /NJH /NJS | Out-Null
robocopy (Join-Path $src 'scripts') (Join-Path $Dest 'scripts') /MIR /NFL /NDL /NJH /NJS | Out-Null
if (Test-Path (Join-Path $src 'frontend\dist')) {
    robocopy (Join-Path $src 'frontend\dist') (Join-Path $Dest 'frontend\dist') /MIR /NFL /NDL /NJH /NJS | Out-Null
} else { Write-Warning 'frontend\dist no existe: ejecuta scripts/wsl/build-frontend.sh en WSL.' }
if ($LASTEXITCODE -ge 8) { throw "robocopy falló ($LASTEXITCODE)" }
Write-Host "Copia lista en $Dest"
