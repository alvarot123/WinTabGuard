$exe = Join-Path $env:LOCALAPPDATA 'WinTabGuard\WinTabGuard.exe'

if (-not (Test-Path -LiteralPath $exe)) {
    exit 1
}

$running = Get-Process -Name 'WinTabGuard' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $exe }

if (-not $running) {
    Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -WindowStyle Hidden
}
