# Inicia Nezzen Music (backend FastAPI en 127.0.0.1, sirviendo el frontend compilado) y abre el navegador.
param([switch]$NoBrowser)
. "$PSScriptRoot\_common.ps1"
Assert-LocalDrive
if (-not (Test-Path $VenvPython)) { throw 'Falta el entorno: ejecuta scripts\windows\setup.ps1' }
$env:NEZZEN_PORT = $Port
$env:NEZZEN_HOST = '127.0.0.1'
if (-not $NoBrowser) {
    Start-Job { param($p) Start-Sleep 2; Start-Process "http://127.0.0.1:$p/" } -ArgumentList $Port | Out-Null
}
Set-Location (Join-Path $Repo 'backend')
& $VenvPython run.py
