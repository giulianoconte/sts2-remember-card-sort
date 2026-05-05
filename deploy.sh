#!/bin/bash
set -ex
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

RELEASE=false
for arg in "$@"; do
    [[ "$arg" == "--release" ]] && RELEASE=true
done

if $RELEASE; then
    DIST_DIR=$SCRIPT_DIR/RememberCardSort/dist
    dotnet publish $SCRIPT_DIR/RememberCardSort/RememberCardSort.csproj --nologo -v quiet -c Release /p:DeployToMods=true /p:ModsPath=$DIST_DIR/
    VERSION=$(python3 -c "import json; print(json.load(open('$SCRIPT_DIR/RememberCardSort/RememberCardSort.json'))['version'])")
    ARCHIVE=$SCRIPT_DIR/RememberCardSort-${VERSION}.zip
    rm -f "$ARCHIVE"
    (cd "$DIST_DIR" && zip -r "$ARCHIVE" RememberCardSort)
    echo "Release archive: $ARCHIVE"
else
    dotnet build $SCRIPT_DIR/RememberCardSort/RememberCardSort.csproj --nologo -v quiet /p:DeployToMods=true
fi
