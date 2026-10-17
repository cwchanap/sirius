# Hoshikawa task runner. Run `just` (no args) to list recipes.

set positional-arguments

# List available recipes (default)
default:
    @just --list

# Build C#, import assets, launch. Extra args go to Godot, e.g. `just run -- --play`
run *args:
    ./run.sh "$@"
