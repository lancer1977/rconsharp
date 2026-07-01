# rconsharp Project Atlas

## Purpose

`rconsharp` implements the Valve RCON protocol as a .NET library.

## Primary Surfaces

- `RconSharp/RconSharp/` - library source and NuGet package metadata.
- `RconSharp/RconSharp.Tests/` - xUnit test suite.
- `RconSharp/RconSharp.sln` - solution entry point.
- `scripts/validate.sh` - repo-native validation command.

## Commands

```bash
bash scripts/validate.sh
dotnet test RconSharp/RconSharp.Tests/RconSharp.Tests.csproj --configuration Release --verbosity minimal
dotnet pack RconSharp/RconSharp/RconSharp.csproj --configuration Release --output ./artifacts
```

## Integration Boundary

Default validation does not require a live game server. Game-specific RCON
compatibility needs an opt-in smoke target with a documented host, port, and
password contract.
