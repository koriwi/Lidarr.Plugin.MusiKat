# Publishing the plugin

Lidarr installs plugins from GitHub releases. The release must meet three
requirements.

## 1. Release asset name

The asset name must contain `net8.0.zip`. Lidarr searches release assets for
the pattern. Our build produces the correct name:

```text
artifacts/Lidarr.Plugin.MusiKat.net8.0.zip
```

## 2. Release notes

Optional but recommended. The release body may contain a minimum Lidarr
version line:

```text
Minimum Lidarr Version: 3.0.0.4855
```

Lidarr parses the pattern `Minimum Lidarr Version: x.y.z.w` (bold markers
allowed). When the running Lidarr is older than the stated version, Lidarr
skips the release. Use the version you developed against (see
`scripts/fetch-lidarr.sh`, the pinned tag).

## 3. Release tag and draft status

- The tag looks like `v1.2.3` or `1.2.3`. Lidarr parses the version from
  the tag.
- Never install a draft release. Publish the release before you install it.

## Install the release

1. Build the plugin: `scripts/build-plugin.sh`.
2. Create a release on this repository.
3. Upload `Lidarr.Plugin.MusiKat.net8.0.zip` as an asset.
4. Publish the release.
5. In Lidarr: System → Plugins → paste this repository URL → Install.

## Versioning

The plugin reads its version from the assembly version in
`src/Lidarr.Plugin.MusiKat/Lidarr.Plugin.MusiKat.csproj`. Bump the version
before each release.
