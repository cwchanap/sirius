# Hoshikawa task runner. Run `just` (no args) to list recipes.

set positional-arguments

# Mono-enabled Godot binary; override with `GODOT_MONO=... just run`
godot := env("GODOT_MONO", "/Applications/Godot_mono.app/Contents/MacOS/Godot")

# List available recipes (default)
default:
    @just --list

# Build C#, import assets, launch. Extra args go to Godot, e.g. `just run -- --play`
run *args:
    dotnet build Hoshikawa.csproj --nologo
    "{{godot}}" --headless --editor --path . --import
    exec "{{godot}}" --path . "$@"

# gdUnit4Net unit tests (tests/unit); plain .NET, no Godot needed. Extra args go to `dotnet test`
unit *args:
    dotnet test tests/unit/Hoshikawa.UnitTests.csproj --nologo "$@"

# The in-engine self-test (scripts/IntegrationCheck.cs), headless
self-test:
    dotnet build Hoshikawa.csproj --nologo
    "{{godot}}" --headless --editor --path . --import
    "{{godot}}" --headless --path . --fixed-fps 60 -- --self-test

# godot-e2e tests (tests/e2e) against a headless game process; failure artifacts land in test_output/
e2e *args:
    dotnet build Hoshikawa.csproj --nologo
    "{{godot}}" --headless --editor --path . --import
    rm -rf test_output
    GODOT_BIN="{{godot}}" dotnet test tests/e2e/Hoshikawa.E2E.csproj --nologo "$@"
