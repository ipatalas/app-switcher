"Resuming at $(Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff")";

$processName = "AppSwitcher"
$proc = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($proc) {
    $oldestProc = $proc | Sort-Object StartTime | Select-Object -First 1
    $pssuspend = $env:SYSINTERNALS + "\PsSuspend.exe"
    if (Test-Path $pssuspend) {
        & $pssuspend -nobanner -r $oldestProc.Id
    } else {
        Write-Output "PsSuspend not found at $pssuspend"
    }
} else {
    Write-Output "Process not running"
}