#!/usr/bin/env bash
# The only parser of Directory.Build.props for shell callers. Source this, don't execute it.
#
#   . build/pins.sh
#   echo "$OPENTOK_PACKAGE_VERSION"

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export OPENTOK_REPO_ROOT
OPENTOK_REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

export OPENTOK_IOS_VERSION
OPENTOK_IOS_VERSION="$(grep -oE '<OpenTokIosVersion>[^<]+' "${OPENTOK_REPO_ROOT}/Directory.Build.props" | head -1 | sed 's/<OpenTokIosVersion>//')"
export OPENTOK_BINDING_REVISION
OPENTOK_BINDING_REVISION="$(grep -oE '<OpenTokBindingRevision>[^<]+' "${OPENTOK_REPO_ROOT}/Directory.Build.props" | head -1 | sed 's/<OpenTokBindingRevision>//')"
export OPENTOK_PACKAGE_VERSION="${OPENTOK_IOS_VERSION}.${OPENTOK_BINDING_REVISION}"
