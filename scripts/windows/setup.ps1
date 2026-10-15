# Crea el entorno virtual en %LOCALAPPDATA% e instala dependencias del backend.
. "$PSScriptRoot\_common.ps1"
Assert-LocalDrive
$py = if (Get-Command py -ErrorAction SilentlyContinue) { 'py' } else { 'python' }
if (-not (Test-Path $VenvPython)) { & $py -3 -m venv $VenvDir; if ($LASTEXITCODE) { throw 'No se pudo crear el venv (¿Python 3.12+ instalado?)' } }
& $VenvPython -m pip install -q -r (Join-Path $Repo 'backend\requirements-dev.txt')
if ($LASTEXITCODE) { throw 'pip install falló' }
Write-Host "Entorno listo: $VenvDir"
