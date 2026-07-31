#!/usr/bin/env bash
# Builds the Lidarr plugin and produces the installable zip.
# The zip contains the full publish output: dll, deps.json, pdb, runtimes.
# The deps.json declares Lidarr.Core/Lidarr.Common as dependencies so the
# plugin resolves them from Lidarr's own assemblies.
# Requires: dotnet SDK 8 or newer, src/Lidarr fetched (scripts/fetch-lidarr.sh).
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$REPO_ROOT/Lidarr.Plugin.MusiKat/Lidarr.Plugin.MusiKat.csproj"
PUBLISH_DIR="$REPO_ROOT/Lidarr.Plugin.MusiKat/bin/Release/net8.0/publish"
ZIP_PATH="$REPO_ROOT/artifacts/Lidarr.Plugin.MusiKat.net8.0.zip"
LIDARR_PROPS="$REPO_ROOT/Lidarr/src/Directory.Build.props"

# The plugin references Lidarr.Core at the AssemblyVersion the Lidarr source
# produces. A local build fills the '10.0.0.*' wildcard with a high version
# that the running Lidarr rejects (its Lidarr.Core is 3.1.3.4987). Pin the
# version to a known-good low value, exactly like the Deemix plugin CI does.
PINNED_VERSION="3.0.0.4855"

if [ ! -d "$REPO_ROOT/Lidarr/.git" ]; then
    echo "Missing Lidarr source. Run scripts/fetch-lidarr.sh first."
    exit 1
fi

cd "$REPO_ROOT"

# Pin the Lidarr AssemblyVersion so the plugin references a version the
# running Lidarr can satisfy.
sed -i "s|<AssemblyVersion>[0-9.]*\*</AssemblyVersion>|<AssemblyVersion>$PINNED_VERSION</AssemblyVersion>|" "$LIDARR_PROPS"
grep -q "$PINNED_VERSION" "$LIDARR_PROPS" || { echo "Failed to pin Lidarr version"; exit 1; }

# EnableAnalyzers=false and TreatWarningsAsErrors=false avoid StyleCop
# failures in Lidarr's own projects under newer .NET SDKs.
dotnet publish "$PROJECT" \
    -c Release \
    -f net8.0 \
    -p:EnableAnalyzers=false \
    -p:TreatWarningsAsErrors=false

python3 "$REPO_ROOT/scripts/patch-deps.py" "$PUBLISH_DIR/Lidarr.Plugin.MusiKat.deps.json"

mkdir -p "$REPO_ROOT/artifacts"
rm -f "$ZIP_PATH"
python3 - "$PUBLISH_DIR" "$ZIP_PATH" <<'PYEOF'
import sys
import zipfile
from pathlib import Path

publish_dir, zip_path = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
    for file in sorted(Path(publish_dir).rglob("*")):
        if file.is_file():
            zf.write(file, file.relative_to(publish_dir))
print(f"Zipped {publish_dir} -> {zip_path}")
PYEOF

echo
echo "Plugin zip: $ZIP_PATH"
