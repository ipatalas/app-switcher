if (Get-Process -Name 'AppSwitcher' -ErrorAction SilentlyContinue) {
  Write-Error "AppSwitcher is currently running. Please close it before uninstalling."
  throw
}

$ErrorActionPreference = 'Stop'

$packageArgs = @{
  packageName    = $env:ChocolateyPackageName
  softwareName   = 'AppSwitcher*'
  fileType       = 'exe'
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
  validExitCodes = @(0)
}

[array]$key = Get-UninstallRegistryKey -SoftwareName $packageArgs['softwareName']

if ($key.Count -eq 1) {
  $key | ForEach-Object {
    $packageArgs['file'] = "$($_.UninstallString)"
    Uninstall-ChocolateyPackage @packageArgs
  }
} elseif ($key.Count -eq 0) {
  Write-Warning "AppSwitcher was not found in registry. It may have been uninstalled manually."
} else {
  Write-Warning "Multiple AppSwitcher installations found. Please uninstall manually."
}
