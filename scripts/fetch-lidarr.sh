#!/usr/bin/env bash
# Fetches the Lidarr source tree needed to build the plugin.
# The plugin compiles against a pinned Lidarr release tag.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LIDARR_DIR="$REPO_ROOT/Lidarr"
LIDARR_TAG="v3.1.3.4987"

if [ -d "$LIDARR_DIR/.git" ]; then
    echo "Lidarr source already present in $LIDARR_DIR"
    exit 0
fi

echo "Cloning Lidarr $LIDARR_TAG into $LIDARR_DIR ..."
git clone --depth 1 --branch "$LIDARR_TAG" https://github.com/Lidarr/Lidarr.git "$LIDARR_DIR"

echo "Done. Build the plugin with:"
echo "  scripts/build-plugin.sh"
