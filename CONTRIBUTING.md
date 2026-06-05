# Contributing

Thanks for improving WinTabGuard.

## Development

Use Windows and the .NET 8 SDK.

```powershell
dotnet restore .\WinTabGuard.sln
dotnet build .\WinTabGuard.sln
```

For local release testing:

```powershell
.\scripts\Build-Release.ps1
.\scripts\Install-WinTabGuard.ps1
```

## Pull requests

- Keep the utility small and dependency-free.
- Prefer explicit Win32 interop over broad frameworks.
- Include a clear explanation when changing keyboard-hook behavior.
- Do not add telemetry.
