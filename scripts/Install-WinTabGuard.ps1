param(
    [string] $PublishDirectory = (Join-Path $PSScriptRoot '..\artifacts\publish'),
    [switch] $EnableWatchdog
)

$ErrorActionPreference = 'Stop'

$appName = 'WinTabGuard'
$installDirectory = Join-Path $env:LOCALAPPDATA $appName
$sourceExe = Join-Path $PublishDirectory "$appName.exe"
$targetExe = Join-Path $installDirectory "$appName.exe"
$watchdogSource = Join-Path $PSScriptRoot 'Start-WinTabGuardIfMissing.ps1'
$watchdogTarget = Join-Path $installDirectory 'Start-WinTabGuardIfMissing.ps1'
$startupShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) "$appName.lnk"
$taskName = "$appName Watchdog"

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "Published executable not found: $sourceExe"
}

New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null

Get-Process -Name $appName -ErrorAction SilentlyContinue | Stop-Process -Force
Copy-Item -LiteralPath $sourceExe -Destination $targetExe -Force
Copy-Item -LiteralPath $watchdogSource -Destination $watchdogTarget -Force

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($startupShortcut)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $installDirectory
$shortcut.Save()

Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue

if ($EnableWatchdog) {
    $action = New-ScheduledTaskAction `
        -Execute 'powershell.exe' `
        -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$watchdogTarget`""

    $trigger = New-ScheduledTaskTrigger `
        -Once `
        -At (Get-Date).Date `
        -RepetitionInterval (New-TimeSpan -Minutes 1) `
        -RepetitionDuration (New-TimeSpan -Days 3650)

    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -MultipleInstances IgnoreNew `
        -ExecutionTimeLimit (New-TimeSpan -Minutes 2)

    Register-ScheduledTask `
        -TaskName $taskName `
        -Action $action `
        -Trigger $trigger `
        -Settings $settings `
        -Description 'Starts WinTabGuard if it is not running.' `
        -Force | Out-Null

    Start-ScheduledTask -TaskName $taskName
}

Start-Process -FilePath $targetExe -WorkingDirectory $installDirectory -WindowStyle Hidden

Write-Host "$appName installed in $installDirectory"
