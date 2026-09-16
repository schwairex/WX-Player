# Third-party components

WX Player application code: MIT. Codec/runtime components retain their own licenses.

* LibVLCSharp 3.10.1 — LGPL-2.1-or-later. Source and license: https://code.videolan.org/videolan/LibVLCSharp/-/tree/3.10.1
* VideoLAN.LibVLC.Windows 3.0.23.1 — native VLC 3.0.23 build; libVLC core LGPL-2.1-or-later; included plugins can be GPL-2.0-or-later and have additional notices. This is dynamic linking with replaceable DLLs; this notice does not relicense VLC plugins. Source archive: https://download.videolan.org/pub/videolan/vlc/3.0.23/vlc-3.0.23.tar.xz ; packaging: https://github.com/mfkl/libvlc-nuget . For redistribution, preserve bundled licenses and satisfy the source obligations of the exact native build and plugins you distribute. Source availability via these upstream links alone is not a substitute for a redistributor's own obligations.
* Microsoft .NET / WPF / Microsoft.Data.Sqlite / ProtectedData — MIT. https://github.com/dotnet/runtime ; https://github.com/dotnet/wpf ; https://github.com/dotnet/efcore
* SQLite — public domain. https://sqlite.org/copyright.html
* SQLitePCLRaw — Apache-2.0. https://github.com/ericsink/SQLitePCL.raw

The open-film sample playlist contains URLs, not media files. Big Buck Bunny and Elephants Dream: Blender Foundation, CC BY 3.0; https://peach.blender.org/ and https://orange.blender.org/ . Sintel: Blender Foundation, CC BY 3.0; https://durian.blender.org/ . Remote sample availability is not guaranteed.

The HLS example points to Apple's publicly hosted HLS technical test stream; https://developer.apple.com/streaming/examples/ . No Apple media is redistributed in the package.

WX Player does not provide subscription channels, credentials, or subscriptions. Use your own authorized sources.

* Inter 4.1 — Copyright Rasmus Andersson; SIL Open Font License 1.1. Unmodified static OTF fonts are bundled; license: licenses/Inter-OFL-1.1.txt. https://rsms.me/inter/ and https://github.com/rsms/inter/releases/tag/v4.1
* WX Player SVG icons — original artwork included under the application's MIT license.

## Online artwork metadata (1.6.0)

Missing artwork discovery optionally uses these public services at runtime. Provider artwork is kept when supplied. No metadata database or remote artwork collection is bundled with this application.

- TVmaze API: https://www.tvmaze.com/api — metadata under CC BY-SA; attribution links are available in Settings → Library. Matching records retain the TVmaze show URL. TVmaze data remains subject to its own license and is not relicensed under the application's MIT license.
- Wikipedia / MediaWiki PageImages: https://www.mediawiki.org/wiki/Extension:PageImages — records retain the originating article URL. Wikipedia text/data and individual images have their respective licenses and copyright terms; API access does not relicense images.
- IPTV-org API/database: https://github.com/iptv-org/api and https://github.com/iptv-org/database — public channel identification and logo metadata. Logos/trademarks belong to their respective owners.

Cached metadata includes its source and attribution URL. Artwork availability and matching depend on each service's catalog. No account password, playlist URL or playback URL is sent to these metadata services.