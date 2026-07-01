# Code Health

## Current State

`rconsharp` is a .NET RCON protocol library with xUnit tests and NuGet package
metadata.

## Validation

```bash
bash scripts/validate.sh
```

The validation script runs:

- Release xUnit tests
- package vulnerability audit
- NuGet pack into a temporary artifact directory
- DevStudio validation when the CLI is available

The local script sets `DOTNET_ROLL_FORWARD=Major` because this workstation has
.NET 10 installed while the library intentionally targets `net8.0`.

## Runtime Boundary

The default tests are fixture/unit tests. Live server compatibility for specific
games such as CS, Minecraft, or ARK requires a real RCON server and should be
handled as an opt-in integration smoke, not the default CI lane.

## Follow-Ups

- Add an opt-in fake or containerized RCON server smoke if protocol compatibility
  work resumes.
- Decide whether this fork should publish packages or remain an internal support
  library.
