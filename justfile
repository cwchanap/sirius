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
