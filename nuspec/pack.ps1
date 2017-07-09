Param(
  [string]$package
)

if ($package) {
    $packages = @($package)
} else {
    $packages = Get-ChildItem -path *.nuspec | Select-Object -ExpandProperty Name
}

foreach($file in $packages) {
    & nuget pack $file
}
