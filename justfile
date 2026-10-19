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

# Fetch the godot-e2e addon into addons/gdunit_e2e if missing (not versioned; CI clones upstream each run)
fetch-e2e:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -d addons/gdunit_e2e/csharp ]; then
        exit 0
    fi
    tmp=$(mktemp -d)
    trap 'rm -rf "$tmp"' EXIT
    git clone --depth 1 https://github.com/cwchanap/godot-e2e "$tmp/godot-e2e"
    mkdir -p addons
    cp -R "$tmp/godot-e2e/addons/gdunit_e2e" addons/gdunit_e2e
    cp "$tmp/godot-e2e/LICENSE" "$tmp/godot-e2e/NOTICE" addons/gdunit_e2e/

# godot-e2e tests (tests/e2e) against a headless game process; failure artifacts land in test_output/
e2e *args: fetch-e2e
    dotnet build Hoshikawa.csproj --nologo
    "{{godot}}" --headless --editor --path . --import
    rm -rf test_output
    GODOT_BIN="{{godot}}" dotnet test tests/e2e/Hoshikawa.E2E.csproj --nologo "$@"
