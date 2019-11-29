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
	if (!($file -match '\.nupkg$')) {
		throw "package must be the full file name of the nupkg file"
	}

    & nuget push $file $password -source http://home.ugts.org/nuget/api/v2/package
}
