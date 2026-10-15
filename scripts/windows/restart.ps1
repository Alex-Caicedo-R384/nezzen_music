# Detiene el proceso que escucha en el puerto de Nezzen y lo vuelve a iniciar.
. "$PSScriptRoot\_common.ps1"
$conn = Get-NetTCPConnection -LocalPort ([int]$Port) -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($conn) {
    $proc = Get-Process -Id $conn.OwningProcess
    if ($proc.ProcessName -notmatch '^python') { throw "El puerto $Port lo usa '$($proc.ProcessName)'; no se detiene." }
    Stop-Process -Id $proc.Id
    Start-Sleep 1
}
& "$PSScriptRoot\start.ps1" -NoBrowser
