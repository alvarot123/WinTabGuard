$ErrorActionPreference = 'SilentlyContinue'

$appName = 'WinTabGuard'
$installDirectory = Join-Path $env:LOCALAPPDATA $appName
$startupShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) "$appName.lnk"
$taskName = "$appName Watchdog"

Get-Process -Name $appName | Stop-Process -Force
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
Remove-Item -LiteralPath $startupShortcut -Force
Remove-Item -LiteralPath $installDirectory -Recurse -Force

Write-Host "$appName uninstalled."
