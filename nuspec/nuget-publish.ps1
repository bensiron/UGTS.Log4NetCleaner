Param(
  [string]$package,
  [string]$password
)

if ($package) {
    $packages = @($package)
} else {
    throw 'package file path is required'
}

foreach($file in $packages) {
    & nuget push $file -source https://www.nuget.org/api/v2/package
}
