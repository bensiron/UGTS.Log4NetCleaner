Param(
  [string]$package,
  [string]$password
)

if ($package) {
    $packages = @($package)
} else {
    $packages = Get-ChildItem -path *.nupkg | Select-Object -ExpandProperty Name
}

if (!$password) { $password = $env:NugetPassword }
if (!$password) { $password = Read-Host 'nuget server password' }


foreach($file in $packages) {
    & nuget push $file $password -source http://ugtsdev/nuget/api/v2/package
}
