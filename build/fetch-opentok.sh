#!/usr/bin/env bash
set -euo pipefail

# Downloads the OpenTok iOS SDK and, optionally, the media transformers library, staging each
# xcframework into the lib/ directory of the project that binds it.
#
# Vonage does not publish these to Maven-equivalent infrastructure .NET can consume directly — the
# real artifacts live behind the CocoaPods source URLs for the OTXCFramework and
# VonageClientSDKVideoTransformers pods. This script resolves those URLs from the pods' own
# podspecs on CocoaPods' CDN and downloads exactly the zips `pod install` would fetch, without
# requiring CocoaPods or Ruby.
#
# Usage:
#   ./build/fetch-opentok.sh                  # both artifacts, at the version in Directory.Build.props
#   ./build/fetch-opentok.sh 2.34.1           # both, at an explicit version
#   ./build/fetch-opentok.sh --core-only      # just the SDK — enough to build OpenTok.Net.iOS
#
# Both artifacts ride one version line: Vonage publishes the transformers zip at the same version
# as the SDK it belongs to, so there is one version argument rather than two.

cd "$(dirname "$0")"

ROOT="$(cd .. && pwd)"
CHECKSUMS="$ROOT/build/checksums.txt"

CORE_ONLY=false
VERSION=""

for arg in "$@"; do
    case "$arg" in
        --core-only) CORE_ONLY=true ;;
        -*) echo "error: unknown option '$arg'" >&2; exit 2 ;;
        *)  VERSION="$arg" ;;
    esac
done

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

sha256_of() {
    if command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | cut -d' ' -f1
    else
        sha256sum "$1" | cut -d' ' -f1
    fi
}

# CocoaPods CDN shards a pod's spec directory by the first three hex digits of the pod name's
# MD5 (not the version) — the same shard for every version of a given pod.
md5_of() {
    if command -v md5 >/dev/null 2>&1; then
        md5 -q -s "$1"
    else
        printf '%s' "$1" | md5sum | cut -d' ' -f1
    fi
}

# fetch_xcframework <pod> <checksum-key-prefix> <xcframework-name> <destination-lib-dir>
#
# Resolves the pod's source URL, verifies the download against build/checksums.txt, and stages the
# named xcframework into the destination. Every step is fatal: a binding built from an
# unverified archive is exactly what the checksum file exists to prevent.
fetch_xcframework() {
    local pod="$1" key_prefix="$2" xcframework="$3" dest="$4"
    local expected md5 shard podspec_url sdk_url work actual

    expected=$(sed -n "s/^${key_prefix}-${VERSION}[[:space:]]\{1,\}\([0-9a-f]\{64\}\).*/\1/p" "$CHECKSUMS" | head -1)
    if [ -z "$expected" ]; then
        echo "error: no SHA-256 recorded for ${key_prefix}-${VERSION} in $CHECKSUMS" >&2
        echo "       see the instructions at the top of that file for how to add one" >&2
        exit 1
    fi

    md5="$(md5_of "$pod")"
    shard="${md5:0:1}/${md5:1:1}/${md5:2:1}"
    podspec_url="https://cdn.cocoapods.org/Specs/$shard/$pod/$VERSION/$pod.podspec.json"

    echo "Resolving $pod $VERSION download URL from $podspec_url ..."

    work="$(mktemp -d)"
    # shellcheck disable=SC2064  # $work is expanded now on purpose; the loop reassigns it.
    trap "rm -rf '$work'" RETURN

    curl -fsSL -o "$work/podspec.json" "$podspec_url"

    sdk_url=$(python3 -c '
import json, sys
with open(sys.argv[1]) as f:
    spec = json.load(f)
print(spec["source"]["http"])
' "$work/podspec.json")

    if [ -z "$sdk_url" ]; then
        echo "error: could not resolve a source.http URL from $podspec_url" >&2
        exit 1
    fi

    echo "Fetching $pod $VERSION from $sdk_url ..."
    curl -fL -o "$work/sdk.zip" "$sdk_url"

    actual=$(sha256_of "$work/sdk.zip")
    if [ "$actual" != "$expected" ]; then
        echo "error: checksum mismatch for ${key_prefix}-${VERSION}" >&2
        echo "       expected $expected" >&2
        echo "       actual   $actual" >&2
        exit 1
    fi

    unzip -q "$work/sdk.zip" -d "$work/extracted"

    if [ ! -d "$work/extracted/$xcframework" ]; then
        echo "error: $xcframework not found in the downloaded archive" >&2
        exit 1
    fi

    rm -rf "$dest"
    mkdir -p "$dest"
    cp -R "$work/extracted/$xcframework" "$dest/"

    echo "Staged $xcframework into $dest"
}

fetch_xcframework \
    "OTXCFramework" \
    "OpenTok-iOS" \
    "OpenTok.xcframework" \
    "$ROOT/src/OpenTok.Net.iOS/lib"

if [ "$CORE_ONLY" = false ]; then
    fetch_xcframework \
        "VonageClientSDKVideoTransformers" \
        "OpenTok-iOS-Transformers" \
        "OpenTokTransformers.xcframework" \
        "$ROOT/src/OpenTok.Net.Transformers.iOS/lib"
fi

echo
echo "No maccatalyst slice is shipped for either artifact (ios-arm64 and simulator only), so there"
echo "is no OpenTok.Net.Mac. Vonage's separate macOS Video SDK is at 2.31 and is a final release."
echo "See README.md."
