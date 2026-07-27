#!/bin/sh

set -e

# Builds and packs the OpenTok.Net.iOS binding package.
#
# Usage:
#   ./build/BuildNugets.sh                       # at the version in Directory.Build.props
#   ./build/BuildNugets.sh --suffix beta.12.34   # same, with a prerelease suffix appended
#
# Run build/fetch-opentok.sh first — the xcframework is not fetched by this script.
#
# The package is written to ../artifacts.
#
# Each .NET SDK's iOS workload ships reference packs for only two target frameworks - the .NET 9
# band covers net8/net9, the .NET 10 band covers net9/net10 - so this runs two passes and merges
# them. The repository's global.json pins the .NET 9 SDK, so the second pass is invoked from a
# scratch directory carrying its own global.json, since the SDK is resolved from the working
# directory. Same approach as the sibling Net.Agora.iOS repository, minus its loop over several
# package ids — there is only one here.

cd "$(dirname "$0")"

SUFFIX=""
case "${1:-}" in
    "") ;;
    --suffix)
        SUFFIX="${2:?--suffix needs a value}"
        ;;
    *)
        echo "error: unknown argument '$1' (use --suffix for prereleases)" >&2
        exit 2
        ;;
esac

ROOT="$(cd .. && pwd)"
OUTPUT="$ROOT/artifacts"
PROJECT="$ROOT/src/OpenTok.Net.iOS/OpenTok.Net.iOS.csproj"

PASS1_BAND="net9"
PASS2_BAND="net10"
PASS2_SDK="10.0.100"

if [ ! -f "$PROJECT" ]; then
    echo "error: $PROJECT does not exist" >&2
    exit 1
fi

VERSION_ARG=""
if [ -n "$SUFFIX" ]; then
    case "$SUFFIX" in
        *[!A-Za-z0-9.-]*)
            echo "error: invalid suffix '$SUFFIX'" >&2
            exit 1
            ;;
    esac
    VERSION_ARG="-p:VersionSuffix=$SUFFIX"
fi

# NuGet.config declares ./artifacts as a package source, and restore fails outright with NU1301 if
# a local source directory is missing - before anything here has had a chance to create it as an
# output directory. A fresh clone only has it because an empty .gitkeep is committed, so make the
# build independent of that surviving.
mkdir -p "$OUTPUT"

PASS1_DIR="$OUTPUT/.net9-pass"
PASS2_DIR="$OUTPUT/.net10-pass"
rm -rf "$PASS1_DIR" "$PASS2_DIR"

SDK10_DIR="$(mktemp -d)"
trap 'rm -rf "$SDK10_DIR"' EXIT
cat > "$SDK10_DIR/global.json" <<EOF
{ "sdk": { "version": "$PASS2_SDK", "rollForward": "latestFeature" } }
EOF

echo "==> packing OpenTok.Net.iOS ($PASS1_BAND band)"
dotnet pack "$PROJECT" \
    -c Release \
    -p:OpenTokSdkBand="$PASS1_BAND" \
    $VERSION_ARG \
    -o "$PASS1_DIR"

echo "==> packing OpenTok.Net.iOS ($PASS2_BAND band)"
(cd "$SDK10_DIR" && dotnet pack "$PROJECT" \
    -c Release \
    -p:OpenTokSdkBand="$PASS2_BAND" \
    $VERSION_ARG \
    -o "$PASS2_DIR")

echo "==> merging target frameworks"
python3 "$ROOT/build/merge-packages.py" "$PASS1_DIR" "$PASS2_DIR" "$OUTPUT"

rm -rf "$PASS1_DIR" "$PASS2_DIR"
