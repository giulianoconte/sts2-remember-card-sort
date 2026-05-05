#!/bin/bash
# Remove the RememberCardSort mod from the deployed mods folder. Resolves
# ModsPath the same way the csproj does: reads local.props if present,
# otherwise falls back to the in-repo dist/ default. Does NOT touch
# BaseLib/ or any other mod — only the RememberCardSort/ subdir.

set -e
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
LOCAL_PROPS=$SCRIPT_DIR/RememberCardSort/local.props

MODS_PATH=""
if [[ -f "$LOCAL_PROPS" ]]; then
    MODS_PATH=$(grep -oP '(?<=<ModsPath>)[^<]+' "$LOCAL_PROPS" | head -1)
fi

if [[ -z "$MODS_PATH" ]]; then
    MODS_PATH=$SCRIPT_DIR/RememberCardSort/dist/
fi

TARGET="${MODS_PATH%/}/RememberCardSort"

# Belt-and-suspenders: refuse anything that didn't resolve to a path
# whose final component is literally "RememberCardSort", so a malformed
# ModsPath can't escalate into a wider rm.
if [[ "$TARGET" != *"/RememberCardSort" ]]; then
    echo "Refusing to remove unexpected path: $TARGET" >&2
    exit 1
fi

if [[ ! -d "$TARGET" ]]; then
    echo "Nothing to remove at $TARGET"
    exit 0
fi

if rm -rf "$TARGET" 2>/tmp/undeploy.err; then
    echo "Removed $TARGET"
else
    cat /tmp/undeploy.err >&2
    echo "" >&2
    echo "Hint: close Slay the Spire 2 if it's running — files held by the" >&2
    echo "game (DLL/PCK) can't be deleted across the vboxsf share." >&2
    rm -f /tmp/undeploy.err
    exit 1
fi
rm -f /tmp/undeploy.err
