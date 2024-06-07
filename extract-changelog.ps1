$version = $args[0]

if (-not $version) {
	Write-Output "Usage: extract-changelog.ps1 <version>"
	exit 1
}

$version = $version -replace "^v", ""

$output = ""
$found = $false

foreach ($line in Get-Content -Path CHANGELOG.md) {
	if ($found -and $line -match "^## \[") {
		break
	}

	if ($found) {
		$output += $line + "`n"
	}

	if (!$found -and $line -match "^## \[$version\]") {
		$found = $true
		continue
	}
}

$output = $output.TrimEnd()

if ($output.Length -eq 0) {
	Write-Output "No changelog found for version $version"
	exit 1
}

Write-Output $output