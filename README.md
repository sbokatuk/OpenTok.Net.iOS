# OpenTok.Net.iOS

[![NuGet](https://img.shields.io/nuget/v/OpenTok.Net.iOS?label=nuget)](https://www.nuget.org/packages/OpenTok.Net.iOS)
[![Targets: net8.0 | net9.0 | net10.0](https://img.shields.io/badge/targets-net8.0%20%7C%20net9.0%20%7C%20net10.0-512BD4)](#building-locally)
[![OTXCFramework 2.34.1](https://img.shields.io/badge/OTXCFramework-2.34.1-099DFD)](https://cocoapods.org/pods/OTXCFramework)
[![iOS 15.0+](https://img.shields.io/badge/iOS-15.0%2B-lightgrey)](#requirements)
[![Licence: MIT](https://img.shields.io/badge/licence-MIT-green)](LICENSE)

.NET for iOS and .NET MAUI bindings for Vonage's (formerly TokBox's) native OpenTok iOS SDK.

```bash
dotnet add package OpenTok.Net.iOS
```

```csharp
using OpenTok.Net.iOS;

var session = new OTSession(apiKey: "<API_KEY>", sessionId: "<SESSION_ID>", delegate: null);
session.ConnectWithToken("<TOKEN>", out var error);

var publisher = new OTPublisher(delegate: null, settings: new OTPublisherSettings { Name = "publisher" });
session.Publish(publisher, out error);
```

These are raw platform bindings — hand-written against the real `OpenTok.framework` headers,
scoped to what a cross-platform client needs: session lifecycle, connect/disconnect, publish a
local stream, subscribe to a remote one, and the matching delegate callbacks for session,
publisher, subscriber, stream and connection events. See [`ApiDefinition.cs`](src/OpenTok.Net.iOS/ApiDefinition.cs)
for the full bound surface.

---

## How this repository works

The iOS binding used to live inside the [`sbokatuk/OpenTok.Net`](https://github.com/sbokatuk/OpenTok.Net)
monorepo (alongside the Android binding and a MAUI sample); this repository carries it forward on
its own, structured after the sibling [`Net.Agora.iOS`](https://github.com/sbokatuk/Net.Agora.iOS)
repository. The package's version is `<SDK version>.<binding revision>` — see
`Directory.Build.props` for why the two numbers are tracked separately, and for the rule that the
version must always sort above what is already on nuget.org (which is why the revision sat at `702`
while this repository was still on SDK 2.27.1, and why moving to 2.34.1 let it restart at 1).

For a cross-platform API over this package and its Android counterpart — one
`Session`/`Publisher`/`Subscriber` and a ready-made MAUI video view, with no `#if IOS` — see
[`sbokatuk/OpenTok.Net`](https://github.com/sbokatuk/OpenTok.Net). This repository is the raw iOS
binding it sits on.

## Requirements

- **iOS 15.0 or newer.** OTXCFramework 2.34.1's own floor per its podspec, raised from 13.0 by that
  SDK release. An app targeting iOS 13 or 14 cannot take this package; stay on `2.27.1.701` if you
  need one of those.

### What is bound, and why by hand

Objective Sharpie's bundled clang cannot parse modern Xcode SDKs directly against a *static*
xcframework the way it can a plain header set, and — same reasoning as the sibling
[`Net.Agora.iOS`](https://github.com/sbokatuk/Net.Agora.iOS) repository — the safer, reproducible
path is a hand-written `ApiDefinition.cs`/`StructsAndEnums.cs`, generated once against the real
`OpenTok.framework` headers and maintained by hand from there. `OTSession`, `OTPublisherKit` /
`OTPublisher`, `OTSubscriberKit` / `OTSubscriber`, `OTStream`, `OTConnection` and `OTError` are
covered, along with their delegate protocols, the network- and media-link stats types, and the
2.34-era additions (`OTVideoCodecPreference`, bitrate and degradation presets, camera torch and
zoom, caption translation, `forceDisconnect:`). Every member carries the header declaration it was
transcribed from, so the next SDK bump can be diffed against the headers rather than re-derived.
Extend it by reading more of the same headers as more of the SDK is needed.

Three conventions run through the file and are enforced by `tests/OpenTok.Net.iOS.PackageTests`,
because each is a mistake the compiler cannot catch:

- `[DisableDefaultCtor]` on every type whose ObjC `init` is `NS_UNAVAILABLE`. Without it the
  generator emits a parameterless constructor that compiles and then traps.
- Delegate callbacks typed with the *header's* parameter type — `OTPublisherKit`, not
  `OTPublisher`. A screen-share publisher is constructed as the base type, and a callback declared
  over the subclass could not receive it.
- Enum members named and valued from the header. Objective Sharpie's prefix stripping once reduced
  `OTVideoViewScaleBehaviorFit`/`Fill` to members called `t` and `ll`, and several
  `OT*VideoEventReason` values the SDK actually sends had no member at all.

**A single, static framework.** Unlike Agora's several *dynamic* xcframeworks (each shipped
alongside the binding assembly and linked by the consuming app), `OpenTok.xcframework`'s binary is
a plain `ar` archive — a static library. It is therefore **embedded** into this binding assembly
at build time (`NativeReference` without `NoBindingEmbedding`), exactly like the original
`OpenTok.Net.iOS` package did. One consequence: there is no per-target-framework
`.resources.zip` to inspect the way `Net.Agora.iOS`'s package tests do — the payload lives inside
the binding assembly's own native companion object.

**No Mac Catalyst.** `OpenTok.xcframework`'s `Info.plist` lists only `ios-arm64` and
`ios-arm64_x86_64-simulator` slices — no `maccatalyst`. There is no `OpenTok.Net.Mac`.

## Building locally

```sh
./build/fetch-opentok.sh          # downloads + SHA-256-verifies OpenTok.xcframework into src/OpenTok.Net.iOS/lib
./build/BuildNugets.sh            # packs into ./artifacts
dotnet test tests/OpenTok.Net.iOS.PackageTests
```

`fetch-opentok.sh` resolves the download URL from `OTXCFramework`'s own CocoaPods podspec (not
CocoaPods itself — this is a binding project, not a pod consumer) and refuses to proceed if the
downloaded archive's SHA-256 doesn't match `build/checksums.txt` — see that file for how to record
a new version's hash.

No single .NET SDK builds net8, net9 *and* net10 for iOS, so `BuildNugets.sh` packs twice (the
installed SDK's band, then a `net10` pass from a scratch `global.json`) and merges the results —
see `build/merge-packages.py`.

## Tests

**Package tests** run anywhere and inspect the packed `.nupkg` — a binding assembly for every
target framework, the embedded native payload present and above a size floor, iOS device and
simulator slices only, and manifests consistent with the slices actually shipped:

```sh
./build/BuildNugets.sh                                  # produce ./artifacts first
dotnet test tests/OpenTok.Net.iOS.PackageTests
```

**Simulator smoke tests** (`tests/OpenTok.Net.iOS.DeviceTests`) build an app against the packed
package and drive the raw binding on a booted simulator, which is the only way to prove the
native framework actually links and loads. The checks assert the framework is loaded into the
process, construct an `OTSession` with a syntactically valid but unregistered API key/session ID,
create an `OTPublisher`, and tear both down; nothing touches the network and no credentials are
involved:

```sh
./.github/scripts/run-simulator-tests.sh 2.34.1.1 net10.0-ios26.0
```

Building the `net10.0-ios26.0` app (this script, or the device-tests project directly) needs an
Xcode carrying exactly the iOS 26.0 SDK — a newer Xcode (26.1+) fails with "This version of .NET
for iOS (26.0.11017) requires Xcode 26.0". CI selects it explicitly (`.github/actions/select-xcode`);
locally, either install that Xcode side by side or run against `net8.0-ios18.0`/`net9.0-ios18.0`
instead, which build against any recent Xcode. Packing the *binding* itself for net10 (`BuildNugets.sh`)
is unaffected — only building an actual app against that target framework hits this.

## CI

| Workflow | Trigger | What it does |
| --- | --- | --- |
| [`pr.yml`](.github/workflows/pr.yml) | pull request | Packs the package as `<version>-beta.<pr>.<run>`, runs the package tests, builds the MAUI sample against the packed package, runs the simulator smoke tests (net8 and net10 legs in parallel), then publishes the beta to nuget.org. Forked PRs build and test but skip publishing, since they cannot read secrets. |
| [`release.yml`](.github/workflows/release.yml) | tag `v*` | Same build and tests at the tag's version, publishes to nuget.org, then creates a GitHub release — from `docs/release-notes/<version>.md` when a curated file exists, otherwise from the commit log since the previous tag. |

Both call the reusable [`build.yml`](.github/workflows/build.yml), which runs on macOS — iOS
builds have no cross-platform path.

## Sample

`samples/OpenTok.Sample.iOS` is a MAUI app (iOS only) that connects to an OpenTok session and
publishes/subscribes video directly against `OTSession`/`OTPublisher`/`OTSubscriber`: an API
key/session/token entry, Connect/Disconnect/Publish buttons, and local + remote video views backed
by a hand-rolled MAUI handler (`OpenTokVideoView.cs`) — exactly what a consumer skipping a
cross-platform façade has to wire up. It is deliberately the same flow and layout as
`samples/OpenTok.Sample.Android` in the Android binding repository, so the two read side by side.

Running it needs a Vonage Video API key, session id and token; everything up to Connect works
without them. If you would rather not write the handler or the delegate subclasses at all, that is
what [`OpenTok.Net` and `OpenTok.Net.Maui`](https://github.com/sbokatuk/OpenTok.Net) package.

It consumes the packed `OpenTok.Net.iOS` package from `./artifacts` (see `NuGet.config`), so fetch
and pack first:

```sh
./build/fetch-opentok.sh
./build/BuildNugets.sh
dotnet build samples/OpenTok.Sample.iOS -f net9.0-ios18.0 -p:RuntimeIdentifier=iossimulator-arm64
```

Debug is the default configuration and is what you want here too: a Release build triggers full
AOT compilation and linking, which costs real build time for no added signal on whether the
package restores and links correctly.

## Licence

MIT — see [LICENSE](LICENSE). Vonage's own OpenTok SDK is distributed under its own commercial SDK
licence.
