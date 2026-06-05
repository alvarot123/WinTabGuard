$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $root 'src\WinTabGuard\WinTabGuard.csproj'
$output = Join-Path $root 'artifacts\publish'

dotnet publish $project -c Release -r win-x64 -o $output

Write-Host "Published WinTabGuard to $output"
