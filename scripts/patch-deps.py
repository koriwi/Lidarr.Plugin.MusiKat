#!/usr/bin/env python3
"""Adds the Lidarr dependency declarations to the plugin deps.json.

Lidarr loads a plugin assembly in a custom AssemblyLoadContext that uses
AssemblyDependencyResolver. The resolver reads the plugin's deps.json next
to the dll. The deps.json must list Lidarr.Core and Lidarr.Common as
dependencies of the plugin, without library file entries, so the resolver
falls back to Lidarr's own assemblies.

Usage: patch-deps.py <path-to-deps.json>
"""
import json
import sys


def main() -> None:
    path = sys.argv[1]
    with open(path, "r", encoding="utf-8") as fh:
        deps = json.load(fh)

    targets = deps.get("targets", {})
    if not targets:
        print("patch-deps: no targets found", file=sys.stderr)
        sys.exit(1)

    target = next(iter(targets.values()))
    plugin_key = next(
        (k for k in target if k.startswith("Lidarr.Plugin.MusiKat/")), None
    )
    if not plugin_key:
        print("patch-deps: plugin entry not found", file=sys.stderr)
        sys.exit(1)

    dependencies = target[plugin_key].setdefault("dependencies", {})
    dependencies.setdefault("Lidarr.Common", "1.0.0")
    dependencies.setdefault("Lidarr.Core", "1.0.0")

    with open(path, "w", encoding="utf-8") as fh:
        json.dump(deps, fh, indent=2)

    print(f"patch-deps: added Lidarr.Core/Lidarr.Common to {plugin_key}")


if __name__ == "__main__":
    main()
