#!/usr/bin/env bash
set -euo pipefail

# Downloads the OpenTok iOS SDK and stages OpenTok.xcframework into src/OpenTok.Net.iOS/lib.
#
# Vonage does not publish this SDK to Maven-equivalent infrastructure .NET can consume directly —
# the real artifact lives behind the CocoaPods source URL for the OTXCFramework pod. This script
# resolves that URL from the pod's own podspec on CocoaPods' CDN and downloads exactly the zip
# `pod install` would fetch, without requiring CocoaPods or Ruby.
#
# Usage:
#   ./build/fetch-opentok.sh            # the version pinned in Directory.Build.props
#   ./build/fetch-opentok.sh 2.27.1     # an explicit version

cd "$(dirname "$0")"

ROOT="$(cd .. && pwd)"
CHECKSUMS="$ROOT/build/checksums.txt"
DEST="$ROOT/src/OpenTok.Net.iOS/lib"
POD="OTXCFramework"

VERSION="${1:-}"
if [ -z "${VERSION}" ]; then
    VERSION=$(sed -n 's:.*<OpenTokIosVersion>\(.*\)</OpenTokIosVersion>.*:\1:p' "$ROOT/Directory.Build.props" | head -1)
fi

if [ -z "${VERSION}" ]; then
    echo "error: could not read OpenTokIosVersion from $ROOT/Directory.Build.props" >&2
    exit 1
fi

# The version is interpolated into a URL and a path, so reject anything exotic up front.
case "$VERSION" in
    *[!A-Za-z0-9._-]*)
        echo "error: invalid version '$VERSION'" >&2
        exit 1
        ;;
esac

expected=$(sed -n "s/^OpenTok-iOS-$VERSION[[:space:]]\{1,\}\([0-9a-f]\{64\}\).*/\1/p" "$CHECKSUMS" | head -1)
if [ -z "$expected" ]; then
    echo "error: no SHA-256 recorded for OpenTok-iOS-$VERSION in $CHECKSUMS" >&2
    echo "       see the instructions at the top of that file for how to add one" >&2
    exit 1
fi

sha256_of() {
    if command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | cut -d' ' -f1
    else
        sha256sum "$1" | cut -d' ' -f1
    fi
}

# CocoaPods CDN shards a pod's spec directory by the first three hex digits of the pod name's
# MD5 (not the version) — the same shard for every version of OTXCFramework.
md5_of() {
    if command -v md5 >/dev/null 2>&1; then
        md5 -q -s "$1"
    else
        printf '%s' "$1" | md5sum | cut -d' ' -f1
    fi
}

md5="$(md5_of "$POD")"
shard="${md5:0:1}/${md5:1:1}/${md5:2:1}"
podspec_url="https://cdn.cocoapods.org/Specs/$shard/$POD/$VERSION/$POD.podspec.json"

echo "Resolving $POD $VERSION download URL from $podspec_url ..."

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

curl -fsSL -o "$WORK/podspec.json" "$podspec_url"

sdk_url=$(python3 -c '
import json, sys
with open(sys.argv[1]) as f:
    spec = json.load(f)
print(spec["source"]["http"])
' "$WORK/podspec.json")

if [ -z "$sdk_url" ]; then
    echo "error: could not resolve a source.http URL from $podspec_url" >&2
    exit 1
fi

echo "Fetching OpenTok iOS SDK $VERSION from $sdk_url ..."

curl -fL -o "$WORK/sdk.zip" "$sdk_url"

actual=$(sha256_of "$WORK/sdk.zip")
if [ "$actual" != "$expected" ]; then
    echo "error: checksum mismatch for OpenTok-iOS-$VERSION.zip" >&2
    echo "       expected $expected" >&2
    echo "       actual   $actual" >&2
    exit 1
fi

unzip -q "$WORK/sdk.zip" -d "$WORK/extracted"

if [ ! -d "$WORK/extracted/OpenTok.xcframework" ]; then
    echo "error: OpenTok.xcframework not found in the downloaded SDK" >&2
    exit 1
fi

rm -rf "$DEST"
mkdir -p "$DEST"
cp -R "$WORK/extracted/OpenTok.xcframework" "$DEST/"

echo "Staged OpenTok.xcframework into $DEST"
echo "No maccatalyst slice is shipped for this SDK (ios-arm64 and simulator only), so there is no"
echo "OpenTok.Net.Mac project. See README.md."
