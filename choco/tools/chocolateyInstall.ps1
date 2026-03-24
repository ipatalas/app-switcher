$ErrorActionPreference = 'Stop'

$url = "https://github.com/ipatalas/app-switcher/releases/download/v$($env:ChocolateyPackageVersion)/AppSwitcher.Installer.exe"

$packageArgs = @{
  packageName    = $env:ChocolateyPackageName
  fileType       = 'exe'
  url            = $url
  checksum       = '__CHECKSUM__'
  checksumType   = 'sha256'
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
  validExitCodes = @(0)
}

Install-ChocolateyPackage @packageArgs
