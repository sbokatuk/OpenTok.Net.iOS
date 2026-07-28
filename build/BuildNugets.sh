#!/bin/sh

set -e

# Builds and packs every package listed in build/packages.tsv: the OpenTok.Net.iOS binding and the
# OpenTok.Net.Transformers.iOS payload package.
#
# Usage:
#   ./build/BuildNugets.sh                       # at the version in Directory.Build.props
#   ./build/BuildNugets.sh --suffix beta.12.34   # same, with a prerelease suffix appended
#
# Run build/fetch-opentok.sh first — no xcframework is fetched by this script. Packing only the
# binding, without the transformers framework staged, therefore fails; use `fetch-opentok.sh
# --core-only` and pack that project directly if that is what you want.
#
# Packages are written to ../artifacts.
#
# Each .NET SDK's iOS workload ships reference packs for only two target frameworks - the .NET 9
# band covers net8/net9, the .NET 10 band covers net9/net10 - so this runs two passes per package
# and merges them. The repository's global.json pins the .NET 9 SDK, so the second pass is invoked
# from a scratch directory carrying its own global.json, since the SDK is resolved from the working
# directory. Same approach as the sibling Net.Agora.iOS repository.

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

PASS1_BAND="net9"
PASS2_BAND="net10"
PASS2_SDK="10.0.100"

# Column 1 is the package id, which is also the directory under src/ and the .csproj basename.
PACKAGES=$(grep -v '^#' packages.tsv | grep -v '^[[:space:]]*$' | cut -f1)

if [ -z "$PACKAGES" ]; then
    echo "error: no packages found in build/packages.tsv" >&2
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

# Packed and merged one package at a time. Nothing here depends on anything else here — the two
# packages are independent by design, see src/OpenTok.Transformers.md — so the order is only about
# keeping the log readable, but merging per package rather than once at the end keeps the two
# pass directories small: the transformers payload alone is over 100 MB per band.
for package in $PACKAGES; do
    project="$ROOT/src/$package/$package.csproj"

    if [ ! -f "$project" ]; then
        echo "error: $project does not exist, but build/packages.tsv lists $package" >&2
        exit 1
    fi

    rm -rf "$PASS1_DIR" "$PASS2_DIR"

    echo "==> packing $package ($PASS1_BAND band)"
    dotnet pack "$project" \
        -c Release \
        -p:OpenTokSdkBand="$PASS1_BAND" \
        $VERSION_ARG \
        -o "$PASS1_DIR"

    echo "==> packing $package ($PASS2_BAND band)"
    (cd "$SDK10_DIR" && dotnet pack "$project" \
        -c Release \
        -p:OpenTokSdkBand="$PASS2_BAND" \
        $VERSION_ARG \
        -o "$PASS2_DIR")

    echo "==> merging target frameworks for $package"
    python3 "$ROOT/build/merge-packages.py" "$PASS1_DIR" "$PASS2_DIR" "$OUTPUT"
done

rm -rf "$PASS1_DIR" "$PASS2_DIR"
