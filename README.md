# Lidarr.Plugin.MusiKat

A [Lidarr](https://lidarr.audio) plugin that downloads music through
[MusiKat](https://github.com/koriwi/musikat) from YouTube.

Lidarr treats MusiKat like a torrent or usenet source. Lidarr decides which
album to grab, when to download it, and when to import it. Delay profiles,
release profiles, and upgrades work normally.

The plugin registers a new download protocol in Lidarr (v3, plugins build):

1. **Indexer** — queries MusiKat's album search. Returns one release per
   album.
2. **Download client** — starts the album download in MusiKat. MusiKat
   writes the tracks into a shared staging folder.
3. **Lidarr import** — Lidarr sees the completed files, matches them to the
   album, and moves them into the library.

The MusiKat fork at [koriwi/musikat](https://github.com/koriwi/musikat)
adds the API endpoints this plugin needs.

## Requirements

- Lidarr v3 (develop channel, 3.1.x or newer) with plugin support enabled
- .NET SDK 8 or newer to build
- A running MusiKat instance (the patched fork)

## Build

```bash
scripts/fetch-lidarr.sh   # once: clones the pinned Lidarr source
scripts/build-plugin.sh
```

The installable zip appears at `artifacts/Lidarr.Plugin.MusiKat.net8.0.zip`.

## Install

### Via GitHub release

1. Build the plugin.
2. Create a release on this repository.
3. Upload `Lidarr.Plugin.MusiKat.net8.0.zip` as an asset.
4. In Lidarr: System → Plugins → paste this repository URL → Install.

See [docs/RELEASING.md](docs/RELEASING.md).

### Manual

1. Unzip the artifact.
2. Create the folder `<config>/plugins/koriwi/MusiKat/`.
3. Copy `Lidarr.Plugin.MusiKat.dll` into that folder.
4. Restart Lidarr.

## Configure

1. Add the MusiKat indexer (Settings → Indexers). Set the Base URL and the
   audio format.
2. Add the MusiKat download client (Settings → Download Clients). Set the
   Base URL and the Download Root (the shared staging path on the MusiKat
   server, for example `/downloads/musikat`).
3. Enable the MusiKat protocol in the default delay profile.

The download root must appear in the MusiKat environment
(`NAVIDROME_MUSIC_PATHS`). Share the folder with the Lidarr container and
set up a remote path mapping when the container paths differ.

## License

MIT
