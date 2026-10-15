. "$PSScriptRoot\_common.ps1"
Assert-LocalDrive
Set-Location (Join-Path $Repo 'backend')
& $VenvPython -m pytest -q
