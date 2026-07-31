#!/usr/bin/env bash
# Builds the Lidarr plugin and produces the installable zip.
# Requires: dotnet SDK 8 or newer, src/Lidarr fetched (scripts/fetch-lidarr.sh).
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$REPO_ROOT/Lidarr.Plugin.MusiKat/Lidarr.Plugin.MusiKat.csproj"

if [ ! -d "$REPO_ROOT/Lidarr/.git" ]; then
    echo "Missing Lidarr source. Run scripts/fetch-lidarr.sh first."
    exit 1
fi

cd "$REPO_ROOT"

# EnableAnalyzers=false and TreatWarningsAsErrors=false avoid StyleCop
# failures in Lidarr's own projects under newer .NET SDKs.
dotnet publish "$PROJECT" \
    -c Release \
    -f net8.0 \
    -p:EnableAnalyzers=false \
    -p:TreatWarningsAsErrors=false

echo
echo "Plugin zip: $REPO_ROOT/artifacts/Lidarr.Plugin.MusiKat.net8.0.zip"
