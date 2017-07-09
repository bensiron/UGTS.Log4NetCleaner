Param(
  [string]$package
)

if ($package) {
    $packages = @($package)
} else {
    $packages = Get-ChildItem -path *.nupkg | Select-Object -ExpandProperty Name
}

$localPath = "C:\share\local\nuget\packages"

if (!(Test-Path $localPath)) {
    New-Item $localPath -type directory
}

foreach($file in $packages) {
    Copy-Item $file -Destination $localPath -Force
}
